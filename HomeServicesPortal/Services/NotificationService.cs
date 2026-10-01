using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class NotificationService : INotificationService
{
    private const string DefaultClickAction = "FLUTTER_NOTIFICATION_CLICK";

    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

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

    public async Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default)
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            _logger.LogWarning("FCM is not configured (Firebase:ServiceAccountPath); push notification skipped.");
            return false;
        }

        var data = new Dictionary<string, string>(dataPayload ?? new Dictionary<string, string>());
        data.TryAdd("click_action", DefaultClickAction);
        data.TryAdd("booking_id", string.Empty);
        data.TryAdd("screen", string.Empty);

        // Message.Token is flagged obsolete in favour of Fid (Firebase Installation ID), but the Flutter
        // app registers FCM registration tokens (getToken()), which is exactly what Token carries.
#pragma warning disable CS0618
        var message = new Message
        {
            Token = deviceToken,
            Notification = new Notification { Title = title, Body = body },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ClickAction = data["click_action"],
                    Sound = "default"
                }
            },
            Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    ["apns-priority"] = "10",
                    ["apns-push-type"] = "alert"
                },
                Aps = new Aps { Sound = "default" }
            }
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
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _db.UserDeviceTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
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
}
