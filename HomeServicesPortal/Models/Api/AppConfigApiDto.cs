using System.Text.Json.Serialization;

namespace HomeServicesPortal.Models.Api;

/// <summary>Bare snake_case payload (no ApiResponse envelope) so the app can parse it before login.</summary>
public class AppConfigApiDto
{
    [JsonPropertyName("minimum_required_version")]
    public string MinimumRequiredVersion { get; set; } = string.Empty;

    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; } = string.Empty;

    [JsonPropertyName("force_update")]
    public bool ForceUpdate { get; set; }

    [JsonPropertyName("store_url")]
    public string StoreUrl { get; set; } = string.Empty;

    [JsonPropertyName("update_message")]
    public string UpdateMessage { get; set; } = string.Empty;

    /// <summary>UTC ISO 8601 ("...Z") of the latest admin release that applies to the caller, or null. Always serialized.</summary>
    [JsonPropertyName("last_unblock_at")]
    public string? LastUnblockAt { get; set; }
}
