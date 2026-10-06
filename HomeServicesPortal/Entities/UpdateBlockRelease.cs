namespace HomeServicesPortal.Entities;

/// <summary>
/// Maps to dbo.UpdateBlockReleases: one row per admin "release blocked devices" send (silent app_unblock push).
/// Append-only history, rows are never edited or deleted. ReleasedAtUtc is the same instant sent as sent_at.
/// </summary>
public class UpdateBlockRelease
{
    public int Id { get; set; }

    /// <summary>everyone | android | ios | user | device.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>"android" | "ios" for the platform scopes, null otherwise.</summary>
    public string? Platform { get; set; }

    /// <summary>UsersLogin.UID for the "user" scope.</summary>
    public int? UserId { get; set; }

    /// <summary>UserDeviceTokens.Id for the "device" scope.</summary>
    public int? DeviceTokenId { get; set; }

    public DateTime ReleasedAtUtc { get; set; }

    /// <summary>Admin portal username who sent it.</summary>
    public string ReleasedBy { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int Recipients { get; set; }

    public int Sent { get; set; }

    public int Failed { get; set; }
}
