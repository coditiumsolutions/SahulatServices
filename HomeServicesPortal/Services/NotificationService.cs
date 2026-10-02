using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class NotificationService : INotificationService
{
    private const string DefaultClickAction = "FLUTTER_NOTIFICATION_CLICK";
    private const int MulticastBatchSize = 500; // FCM limit per multicast call.

    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
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

        // Message.Token is flagged obsolete in favour of Fid (Firebase Installation ID), but the Flutter
        // app registers FCM registration tokens (getToken()), which is exactly what Token carries.
#pragma warning disable CS0618
        var message = new Message
        {
            Token = deviceToken,
            Notification = new Notification { Title = title, Body = body },
            Data = data,
            Android = BuildAndroid(),
            Apns = BuildApns()
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
        return await SendToDevicesAsync(tokens, title, body, dataPayload, cancellationToken);
    }

    public async Task<BroadcastResult> SendToDevicesAsync(IReadOnlyCollection<string> deviceTokens, string title,
        string body, Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("FCM is not configured (Firebase:ServiceAccountPath); send skipped.");
            return new BroadcastResult(false, 0, 0, 0, 0);
        }

        var tokens = deviceTokens.Distinct().ToList();
        var data = BuildData(dataPayload);
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
                Android = BuildAndroid(),
                Apns = BuildApns()
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
        data.TryAdd("click_action", DefaultClickAction);
        data.TryAdd("booking_id", string.Empty);
        data.TryAdd("screen", string.Empty);
        return data;
    }

    // No AndroidNotification.ClickAction on purpose: a click action makes Android launch an intent with that
    // action name, and the app has no activity filtering for it, so the tap would do nothing. Without it a tap
    // opens the launcher activity and Flutter's onMessageOpenedApp / getInitialMessage receive the data payload.
    private static AndroidConfig BuildAndroid() => new()
    {
        Priority = Priority.High,
        Notification = new AndroidNotification
        {
            Sound = "default",
            // Explicit event time (server UTC) so the banner's "2m ago" doesn't depend on the device's own stamp.
            EventTimestamp = DateTime.UtcNow
        }
    };

    private static ApnsConfig BuildApns() => new()
    {
        Headers = new Dictionary<string, string>
        {
            ["apns-priority"] = "10",
            ["apns-push-type"] = "alert"
        },
        Aps = new Aps { Sound = "default" }
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
