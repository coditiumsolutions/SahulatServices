using System.Text.RegularExpressions;
using HomeServicesPortal.Options;

namespace HomeServicesPortal.Helpers;

/// <summary>
/// Builds the FCM data map of a type = app_update push for ONE platform. Android and iOS ship separate store
/// releases, so the version and store link are always taken from that platform's AppConfig policy (the same
/// settings GET /api/v1/app/config reads) and never shared between platforms.
/// </summary>
public static partial class AppUpdatePayload
{
    public static readonly string[] Platforms = { "android", "ios" };

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex VersionPattern();

    public static AppVersionPolicy PolicyFor(AppConfigOptions config, string platform) =>
        platform == "ios" ? config.Ios : config.Android;

    /// <summary>
    /// latest_version must be the store-facing major.minor.patch (no build number or suffix). On failure
    /// <paramref name="error"/> names the platform and the problem so staff can fix the setting.
    /// </summary>
    public static bool TryBuild(AppConfigOptions config, string platform, out Dictionary<string, string> data,
        out string? error, string? versionOverride = null, bool forceUpdate = true)
    {
        var policy = PolicyFor(config, platform);
        var label = platform == "ios" ? "iOS" : "Android";
        var version = (versionOverride ?? policy.LatestVersion ?? string.Empty).Trim();

        data = new Dictionary<string, string>();
        if (version.Length == 0)
        {
            error = $"{label} has no latest_version: enter one (the form is pre-filled from AppConfig:{(platform == "ios" ? "Ios" : "Android")}:LatestVersion). Nothing was sent.";
            return false;
        }

        if (!VersionPattern().IsMatch(version))
        {
            error = $"{label} latest_version \"{version}\" is not in major.minor.patch form (e.g. 1.0.5). Nothing was sent.";
            return false;
        }

        data = new Dictionary<string, string>
        {
            ["type"] = NotificationTypes.AppUpdate,
            ["screen"] = NotificationScreens.AppUpdate,
            ["latest_version"] = version,
            ["store_url"] = (policy.StoreUrl ?? string.Empty).Trim(),
            // Always present, exactly "true" or "false": a build that does not know the key blocks, as before.
            ["force_update"] = forceUpdate ? "true" : "false",
            ["platform"] = platform
        };
        error = null;
        return true;
    }
}
