namespace HomeServicesPortal.Options;

/// <summary>
/// OpenStreetMap Nominatim reverse-geocoding settings. Free, no API key required.
/// BaseUrl is configurable so this can later point at a self-hosted Nominatim instance.
/// </summary>
public class NominatimOptions
{
    public const string SectionName = "Nominatim";

    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>Required by Nominatim's usage policy — must identify the app, not be blank/generic.</summary>
    public string UserAgent { get; set; } = "SahulatGharTak/1.0 (contact: coditiumsolutions@gmail.com)";

    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Minimum gap enforced between outgoing requests, to respect Nominatim's ~1 req/s policy.</summary>
    public int MinRequestIntervalMs { get; set; } = 1100;
}
