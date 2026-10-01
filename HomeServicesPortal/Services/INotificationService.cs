namespace HomeServicesPortal.Services;

public interface INotificationService
{
    /// <summary>Insert or update the token row. A token already owned by another user is re-assigned.</summary>
    Task UpsertDeviceTokenAsync(int userId, string userType, string deviceToken, string platform,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send one FCM message. Returns false (never throws) when FCM is not configured or delivery fails.
    /// A token FCM reports as Unregistered is deleted from the DB.
    /// </summary>
    Task<bool> SendPushNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default);

    /// <summary>Send to every registered device of a UsersLogin user. Returns the number of successful sends.</summary>
    Task<int> SendPushNotificationToUserAsync(int userId, string title, string body,
        Dictionary<string, string>? dataPayload = null, CancellationToken cancellationToken = default);
}
