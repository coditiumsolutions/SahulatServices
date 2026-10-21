using System.Text.Json;
using HomeServicesPortal.Helpers;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

public class AppLinkFilesTests
{
    [Fact]
    public void AssetLinks_names_the_android_package_and_a_well_formed_fingerprint()
    {
        using var doc = JsonDocument.Parse(AppLinkFiles.BuildAssetLinks());
        var target = doc.RootElement[0].GetProperty("target");

        Assert.Equal("android_app", target.GetProperty("namespace").GetString());
        Assert.Equal(AppLinkFiles.AndroidPackage, target.GetProperty("package_name").GetString());
        Assert.All(AppLinkFiles.AndroidSha256Fingerprints,
            f => Assert.Matches("^([0-9A-F]{2}:){31}[0-9A-F]{2}$", f));
    }

    [Fact]
    public void AppleAssociation_lists_the_app_id_and_keeps_legal_pages_on_the_web()
    {
        var json = AppLinkFiles.BuildAppleAppSiteAssociation();
        using var doc = JsonDocument.Parse(json);
        var detail = doc.RootElement.GetProperty("applinks").GetProperty("details")[0];

        Assert.Equal(AppLinkFiles.IosAppId, detail.GetProperty("appIDs")[0].GetString());
        Assert.Contains("\"/\":\"/\"", json);
        Assert.Contains("\"/\":\"/app*\"", json);
        Assert.DoesNotContain("privacy-policy", json);
        Assert.DoesNotContain("delete-account", json);
        Assert.DoesNotContain("adminportal", json, StringComparison.OrdinalIgnoreCase);
    }
}
