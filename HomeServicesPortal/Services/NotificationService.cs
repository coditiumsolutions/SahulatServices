using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

public class NotificationService : INotificationService
{
    private const string DefaultClickAction = "FLUTTER_NOTIFICATION_CLICK";
    private const int MulticastBatchSize = 500; // FCM limit per multicast call.

    // Brand navy (--hs-navy): tints the small icon and the app-name line of the Android notification.
    private const string AccentColor = "#003366";

    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;
    private readonly IOptionsMonitor<NotificationOptions> _options;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger,
        IOptionsMonitor<NotificationOptions> options)
    {
        _db = db;
        _logger = logger;
        _options = options;
    }

    // ---- device tokens ------------------------------------------------------------------------

    public async Task UpsertDeviceTokenAsync(int userId, string userType, string deviceToken, string platform,
        CancellationToken cancellationToken = default)
    {
        // Retried registrations and two racing inserts of the same token must converge on one row:
        // DeviceToken has a unique index, so a lost insert race falls back to the update path.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var row = await _db.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.DeviceToken == deviceToken, cancellationToken);

            if (row == null)
            {
                _db.UserDeviceTokens.Add(new UserDeviceToken
                {
                    UserId = userId,
                    UserType = userType,
                    DeviceToken = deviceToken,
                    Platform = platform,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                row.UserId = userId;
                row.UserType = userType;
                row.Platform = platform;
                row.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException) when (row == null && attempt == 0)
            {
                _db.ChangeTracker.Clear();
            }
        }
    }

    public async Task RemoveDeviceTokenAsync(string deviceToken, CancellationToken cancellationToken = default)
    {
        await _db.UserDeviceTokens
            .Where(t => t.DeviceToken == deviceToken)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<bool> UserExistsAsync(int userId, string userType, CancellationToken cancellationToken = default)
    {
        // Check the role's own profile row, not UsersLogin.UserType: an account upgraded from client to provider
        // is stored as "Provider" there but keeps its Clients row, and can register a device for either role.
        return userType == Helpers.UserTypeConstants.Client
            ? _db.UsersLogins.AsNoTracking().AnyAsync(u => u.Uid == userId && u.IsActive
                && _db.Clients.Any(c => c.UserUid == userId), cancellationToken)
            : _db.UsersLogins.AsNoTracking().AnyAsync(u => u.Uid == userId && u.IsActive
                && _db.Providers.Any(p => p.UserUid == userId), cancellationToken);
    }

    // ---- sending ------------------------------------------------------------------------------

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("FCM is not configured (Firebase:ServiceAccountPath); push notification skipped.");
            return false;
        }

        var data = BuildData(dataPayload);
        var channelsEnabled = _options.CurrentValue.AndroidChannelsEnabled;

        // Message.Token is flagged obsolete in favour of Fid (Firebase Installation ID), but the Flutter
        // app registers FCM registration tokens (getToken()), which is exactly what Token carries.
#pragma warning disable CS0618
        var message = new Message
        {
            Token = deviceToken,
            Notification = new Notification { Title = title, Body = body },
            Data = data,
            Android = BuildAndroid(data, channelsEnabled),
            Apns = BuildApns(data)
        };
#pragma warning restore CS0618

        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            _logger.LogInformation("FCM token is unregistered; removing it from UserDeviceTokens.");
            await _db.UserDeviceTokens
                .Where(t => t.DeviceToken == deviceToken)
                .ExecuteDeleteAsync(cancellationToken);
            return false;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "FCM send failed ({ErrorCode}).", ex.MessagingErrorCode);
            return false;
        }
    }

    public async Task<int> SendPushNotificationToUserAsync(int userId, string title, string body,
        Dictionary<string, string>? dataPayload = null, string? userType = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && (userType == null || t.UserType == userType))
            .Select(t => t.DeviceToken)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var token in tokens)
        {
            if (await SendPushNotificationAsync(token, title, body, dataPayload, cancellationToken))
            {
                sent++;
            }
        }

        return sent;
    }

    public async Task NotifyUserAsync(int userId, string userType, string type, string title, string body,
        string screen, int? bookingUid = null, int? requestUid = null, CancellationToken cancellationToken = default)
    {
        var row = new UserNotification
        {
            UserId = userId,
            UserType = userType,
            Type = type,
            Title = Truncate(title, 200),
            Body = Truncate(body, 500),
            Screen = screen,
            BookingUid = bookingUid,
            RequestUid = requestUid,
            CreatedAt = DateTime.UtcNow
        };
        _db.UserNotifications.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        var data = new Dictionary<string, string>
        {
            ["type"] = type,
            ["screen"] = screen,
            ["booking_id"] = bookingUid?.ToString() ?? string.Empty,
            ["request_id"] = requestUid?.ToString() ?? string.Empty,
            ["notification_id"] = row.Id.ToString()
        };

        await SendPushNotificationToUserAsync(userId, row.Title, row.Body, data, userType, cancellationToken);
        await PruneInboxAsync(userId, userType, cancellationToken);
    }

    /// <summary>
    /// Enforces the inbox limits (Configurations: Inbox.RetentionDays, Inbox.MaxPerRole) for one user and role.
    /// Runs after each new row so there is no background job; failure is logged and never fails the notification.
    /// </summary>
    private async Task PruneInboxAsync(int userId, string userType, CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _db.Configurations.AsNoTracking()
                .Where(c => c.ConfigKey == Helpers.InboxRetention.DaysKey || c.ConfigKey == Helpers.InboxRetention.MaxPerRoleKey)
                .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue, StringComparer.OrdinalIgnoreCase, cancellationToken);

            settings.TryGetValue(Helpers.InboxRetention.DaysKey, out var daysRaw);
            settings.TryGetValue(Helpers.InboxRetention.MaxPerRoleKey, out var maxRaw);
            var days = Helpers.InboxRetention.ParseOrDefault(daysRaw, Helpers.InboxRetention.DefaultDays,
                Helpers.InboxRetention.MinDays, Helpers.InboxRetention.MaxDays);
            var max = Helpers.InboxRetention.ParseOrDefault(maxRaw, Helpers.InboxRetention.DefaultMaxPerRole,
                Helpers.InboxRetention.MinMaxPerRole, Helpers.InboxRetention.MaxMaxPerRole);

            var cutoff = DateTime.UtcNow.AddDays(-days);
            await _db.UserNotifications
                .Where(n => n.UserId == userId && n.UserType == userType && n.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            // Everything past the newest `max` rows. Pruned on every write, so this is a handful of ids at most.
            var overflow = await _db.UserNotifications.AsNoTracking()
                .Where(n => n.UserId == userId && n.UserType == userType)
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Skip(max)
                .Select(n => n.Id)
                .ToListAsync(cancellationToken);
            if (overflow.Count > 0)
            {
                await _db.UserNotifications.Where(n => overflow.Contains(n.Id)).ExecuteDeleteAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inbox pruning failed for user {UserId} ({UserType}).", userId, userType);
        }
    }

    public async Task<BroadcastResult> SendBroadcastAsync(string? platform, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("FCM is not configured (Firebase:ServiceAccountPath); broadcast skipped.");
            return new BroadcastResult(false, 0, 0, 0, 0);
        }

        var query = _db.UserDeviceTokens.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(platform) && !platform.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var p = platform.Trim().ToLowerInvariant();
            query = query.Where(t => t.Platform == p);
        }

        var tokens = await query.Select(t => t.DeviceToken).Distinct().ToListAsync(cancellationToken);
        return await SendToDevicesAsync(tokens, title, body, dataPayload, null, cancellationToken);
    }

    public async Task<BroadcastResult> SendToDevicesAsync(IReadOnlyCollection<string> deviceTokens, string title,
        string body, Dictionary<string, string>? dataPayload = null, bool? androidChannels = null,
        CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("FCM is not configured (Firebase:ServiceAccountPath); send skipped.");
            return new BroadcastResult(false, 0, 0, 0, 0);
        }

        var tokens = deviceTokens.Distinct().ToList();
        var data = BuildData(dataPayload);
        var channelsEnabled = androidChannels ?? _options.CurrentValue.AndroidChannelsEnabled;
        int sent = 0, failed = 0;
        var stale = new List<string>();

        foreach (var batch in tokens.Chunk(MulticastBatchSize))
        {
            // Tokens is flagged obsolete in favour of Fids (Firebase Installation IDs); the app registers
            // FCM registration tokens, which is what Tokens carries.
#pragma warning disable CS0618
            var message = new MulticastMessage
            {
                Tokens = batch,
                Notification = new Notification { Title = title, Body = body },
                Data = data,
                Android = BuildAndroid(data, channelsEnabled),
                Apns = BuildApns(data)
            };
#pragma warning restore CS0618

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);
                for (var i = 0; i < response.Responses.Count; i++)
                {
                    var r = response.Responses[i];
                    if (r.IsSuccess)
                    {
                        sent++;
                        continue;
                    }

                    failed++;
                    if (r.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                    {
                        stale.Add(batch[i]);
                    }
                }
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "FCM broadcast batch failed ({ErrorCode}).", ex.MessagingErrorCode);
                failed += batch.Length;
            }
        }

        if (stale.Count > 0)
        {
            await _db.UserDeviceTokens
                .Where(t => stale.Contains(t.DeviceToken))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return new BroadcastResult(true, tokens.Count, sent, failed, stale.Count);
    }

    // ---- inbox --------------------------------------------------------------------------------

    public async Task<UserNotificationListDto> GetInboxAsync(int userId, string? userType, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId && (userType == null || n.UserType == userType));

        var total = await query.CountAsync(cancellationToken);
        var unread = await query.CountAsync(n => !n.IsRead, cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new UserNotificationApiDto
            {
                Id = n.Id,
                UserType = n.UserType,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                Screen = n.Screen,
                BookingUid = n.BookingUid,
                RequestUid = n.RequestUid,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new UserNotificationListDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            UnreadCount = unread
        };
    }

    public Task<int> GetUnreadCountAsync(int userId, string? userType, CancellationToken cancellationToken = default)
    {
        return _db.UserNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead && (userType == null || n.UserType == userType),
                cancellationToken);
    }

    public async Task<bool> MarkReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        // Idempotent: already-read rows still count as found, so a retry returns the same success.
        var exists = await _db.UserNotifications
            .AnyAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (!exists) return false;

        await _db.UserNotifications
            .Where(n => n.Id == notificationId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
        return true;
    }

    public Task<int> MarkAllReadAsync(int userId, string? userType, CancellationToken cancellationToken = default)
    {
        return _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead && (userType == null || n.UserType == userType))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    // ---- helpers ------------------------------------------------------------------------------

    /// <summary>FCM data values must be non-null strings; guarantee the contract keys always exist.</summary>
    private static Dictionary<string, string> BuildData(Dictionary<string, string>? dataPayload)
    {
        var data = new Dictionary<string, string>();
        if (dataPayload != null)
        {
            foreach (var (key, value) in dataPayload)
            {
                data[key] = value ?? string.Empty;
            }
        }

        data.TryAdd("sent_at", DateTime.UtcNow.ToString("O"));
        data.TryAdd("channel_id", NotificationChannels.For(data.GetValueOrDefault("type")));
        data.TryAdd("click_action", DefaultClickAction);
        data.TryAdd("booking_id", string.Empty);
        data.TryAdd("screen", string.Empty);
        return data;
    }

    // No AndroidNotification.ClickAction on purpose: a click action makes Android launch an intent with that
    // action name, and the app has no activity filtering for it, so the tap would do nothing. Without it a tap
    // opens the launcher activity and Flutter's onMessageOpenedApp / getInitialMessage receive the data payload.
    private static AndroidConfig BuildAndroid(Dictionary<string, string> data, bool channelsEnabled)
    {
        var channelId = data.GetValueOrDefault("channel_id") ?? NotificationChannels.BookingUpdates;
        var notification = new AndroidNotification
        {
            Sound = NotificationSounds.Android(channelId) ?? "default",
            Color = AccentColor,
            // Same tag: a newer push for the same booking/request replaces the old banner instead of stacking.
            Tag = ThreadKey(data),
            // Explicit event time (server UTC) so the banner's "2m ago" doesn't depend on the device's own stamp.
            EventTimestamp = DateTime.UtcNow
        };

        // Only name a channel once an app build that creates it is live (NotificationOptions.AndroidChannelsEnabled):
        // an unknown channel id sends the push to Android's generic fallback channel and loses the heads-up banner.
        if (channelsEnabled)
        {
            notification.ChannelId = channelId;
        }

        return new AndroidConfig { Priority = Priority.High, Notification = notification };
    }

    private static ApnsConfig BuildApns(Dictionary<string, string> data) => new()
    {
        Headers = new Dictionary<string, string>
        {
            ["apns-priority"] = "10",
            ["apns-push-type"] = "alert"
        },
        Aps = new Aps
        {
            Sound = NotificationSounds.Ios(data.GetValueOrDefault("channel_id") ?? NotificationChannels.BookingUpdates)
                ?? "default",
            // Groups a booking's notifications together in the iOS notification centre.
            ThreadId = ThreadKey(data)
        }
    };

    /// <summary>"booking-{id}" / "request-{id}" so updates for one job stack and replace together; null for broadcasts.</summary>
    private static string? ThreadKey(Dictionary<string, string> data)
    {
        if (data.TryGetValue("booking_id", out var booking) && !string.IsNullOrEmpty(booking)) return $"booking-{booking}";
        if (data.TryGetValue("request_id", out var request) && !string.IsNullOrEmpty(request)) return $"request-{request}";
        return null;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
