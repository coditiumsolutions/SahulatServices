namespace HomeServicesPortal.Options;

/// <summary>Per-platform app version policy, editable in appsettings without a redeploy.</summary>
public class AppConfigOptions
{
    public const string SectionName = "AppConfig";

    public AppVersionPolicy Android { get; set; } = new();

    public AppVersionPolicy Ios { get; set; } = new();
}

public class AppVersionPolicy
{
    public string MinimumRequiredVersion { get; set; } = "1.0.0";

    public string LatestVersion { get; set; } = "1.0.0";

    public bool ForceUpdate { get; set; }

    public string StoreUrl { get; set; } = string.Empty;

    public string UpdateMessage { get; set; } = "A new version of Sahulat Ghar Tak is available.";
}
