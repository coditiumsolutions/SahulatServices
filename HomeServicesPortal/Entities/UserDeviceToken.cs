namespace HomeServicesPortal.Entities;

/// <summary>
/// Maps to dbo.UserDeviceTokens — FCM registration tokens. UserId is UsersLogin.UID (the JWT "UserId" claim).
/// DeviceToken is unique: a physical device token belongs to exactly one user at a time.
/// </summary>
public class UserDeviceToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Client or Provider.</summary>
    public string UserType { get; set; } = string.Empty;

    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>android or ios.</summary>
    public string Platform { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
