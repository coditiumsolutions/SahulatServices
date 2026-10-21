using System.Text.Json;

namespace HomeServicesPortal.Helpers;

/// <summary>
/// The two verification files that let a phone open the installed app for sahulatghartak.com links
/// (Android App Links: assetlinks.json, iOS Universal Links: apple-app-site-association). The path list must
/// match the Flutter app's Android intent-filter; see "Planned: deep links" in docs/mobile-app/flutter-changes.md.
/// Served from code because static files skip dot-prefixed folders (.well-known) and the iOS file has no extension.
/// </summary>
public static class AppLinkFiles
{
    public const string AndroidPackage = "com.coditiumsols.sahulatghartak";
    public const string IosAppId = "J24H49NXWD.com.coditiumsols.sahulatghartak.ios";

    /// <summary>Public SHA-256 certificate fingerprints (not secrets).</summary>
    public static readonly string[] AndroidSha256Fingerprints =
    [
        // Play App Signing key (Play Console, App integrity): what installed apps are signed with
        "8C:4F:37:31:46:D7:BD:5B:E6:05:A2:D4:4C:91:33:8C:14:C8:23:F5:8C:44:BF:34:AD:84:EE:D8:81:2F:F5:85",
        // Upload key (alias SahulatUpload)
        "41:84:0E:50:4F:EF:CC:10:AD:44:29:4B:29:AF:73:61:0F:9E:CA:54:A6:C2:D7:7C:5B:A2:79:59:CA:D6:BC:62",
    ];

    /// <summary>Exact path "/" plus these prefixes open the app. Privacy, delete-account, about and admin stay on the web.</summary>
    public static readonly string[] PathPrefixes =
    [
        "/app",
        "/home-maintenance",
        "/specialized-services",
        "/property-and-legal-services",
        "/Home/DownloadApp",
    ];

    public static string BuildAssetLinks() => JsonSerializer.Serialize(new object[]
    {
        new
        {
            relation = new[] { "delegate_permission/common.handle_all_urls" },
            target = new
            {
                @namespace = "android_app",
                package_name = AndroidPackage,
                sha256_cert_fingerprints = AndroidSha256Fingerprints,
            },
        },
    });

    public static string BuildAppleAppSiteAssociation()
    {
        var components = new List<Dictionary<string, string>> { new() { ["/"] = "/" } };
        components.AddRange(PathPrefixes.Select(p => new Dictionary<string, string> { ["/"] = p + "*" }));

        return JsonSerializer.Serialize(new
        {
            applinks = new
            {
                details = new[] { new { appIDs = new[] { IosAppId }, components } },
            },
        });
    }
}
