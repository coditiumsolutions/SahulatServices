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

    /// <summary>All category ids this provider was registered with (added 2026-09-21). Contains just [CategoryId] for a single-category registration.</summary>
    public List<int> CategoryIds { get; set; } = new();

    /// <summary>Clients.UID for the account this provider profile was upgraded from — lets the app submit customer service requests immediately after upgrading, without a fresh login.</summary>
    public int? ClientId { get; set; }

    /// <summary>Providers.City — one of the configured city options (see GET /api/cities).</summary>
    public string? City { get; set; }
}
