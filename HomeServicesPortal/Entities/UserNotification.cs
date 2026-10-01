namespace HomeServicesPortal.Entities;

/// <summary>
/// Maps to dbo.UserNotifications — per-user in-app inbox. UserId is UsersLogin.UID. UserType is the role
/// (Client/Provider) the notification was addressed to, so a dual-role account keeps two separate inboxes.
/// </summary>
public class UserNotification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string UserType { get; set; } = string.Empty;

    /// <summary>See <see cref="HomeServicesPortal.Helpers.NotificationTypes"/>.</summary>
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    /// <summary>App route hint, e.g. request_details. See <see cref="HomeServicesPortal.Helpers.NotificationScreens"/>.</summary>
    public string Screen { get; set; } = string.Empty;

    public int? BookingUid { get; set; }

    public int? RequestUid { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}
