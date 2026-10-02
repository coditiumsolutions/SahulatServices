using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public record BroadcastResult(bool FirebaseConfigured, int Recipients, int Sent, int Failed, int RemovedStale);

public interface INotificationService
{
    /// <summary>Insert or update the token row. A token already owned by another user is re-assigned.</summary>
    Task UpsertDeviceTokenAsync(int userId, string userType, string deviceToken, string platform,
        CancellationToken cancellationToken = default);

    /// <summary>Delete a token row (app logout). Idempotent — an unknown token is a no-op.</summary>
    Task RemoveDeviceTokenAsync(string deviceToken, CancellationToken cancellationToken = default);

    /// <summary>True when an active UsersLogin row exists with this id and user type.</summary>
    Task<bool> UserExistsAsync(int userId, string userType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send one FCM message. Returns false (never throws) when FCM is not configured or delivery fails.
    /// A token FCM reports as Unregistered is deleted from the DB.
    /// </summary>
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default);

    /// <summary>Send to every registered device of a UsersLogin user (optionally only one role). Returns successful sends.</summary>
    Task<int> SendPushNotificationToUserAsync(int userId, string title, string body,
        Dictionary<string, string>? dataPayload = null, string? userType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save an inbox row for the user/role and push it to that role's devices. The inbox row is written first,
    /// so a failed push still leaves the notification visible in the app.
    /// </summary>
    Task NotifyUserAsync(int userId, string userType, string type, string title, string body, string screen,
        int? bookingUid = null, int? requestUid = null, CancellationToken cancellationToken = default);

    /// <summary>Push one message to every registered device (all, or one platform). No inbox rows are written.</summary>
    Task<BroadcastResult> SendBroadcastAsync(string? platform, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Push one message to an explicit set of device tokens (staff test tool). No inbox rows are written.
    /// androidChannels overrides Notifications:AndroidChannelsEnabled for this send (null = use the setting).
    /// </summary>
    Task<BroadcastResult> SendToDevicesAsync(IReadOnlyCollection<string> deviceTokens, string title, string body,
        Dictionary<string, string>? dataPayload = null, bool? androidChannels = null,
        CancellationToken cancellationToken = default);

    Task<UserNotificationListDto> GetInboxAsync(int userId, string? userType, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(int userId, string? userType, CancellationToken cancellationToken = default);

    /// <summary>Marks one notification read. Returns false when it doesn't exist or belongs to another user.</summary>
    Task<bool> MarkReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(int userId, string? userType, CancellationToken cancellationToken = default);
}
