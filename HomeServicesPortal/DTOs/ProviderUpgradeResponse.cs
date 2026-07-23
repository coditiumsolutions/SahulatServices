namespace HomeServicesPortal.DTOs;

public class ProviderUpgradeResponse
{
    public int UserId { get; set; }

    /// <summary>Provider profile id (Providers.UID). Kept for backward compatibility.</summary>
    public int ProfileId { get; set; }

    /// <summary>Same as ProfileId — Providers.UID for Flutter document upload APIs.</summary>
    public int ProviderUid { get; set; }

    public string UserType { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string MobileNo { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }
}
