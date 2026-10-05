using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

/// <summary>
/// The effective per-platform app version policy: the AppConfig section of appsettings, with LatestVersion replaced
/// by the value saved in the Configurations table (keys AppConfig.Android.LatestVersion / AppConfig.Ios.LatestVersion)
/// when one exists. Saving to the table needs no file edit or redeploy, and GET /api/v1/app/config and the app_update
/// push both read through this, so they cannot disagree.
/// </summary>
public interface IAppVersionPolicyService
{
    /// <summary>appsettings AppConfig with the saved LatestVersion overrides applied (a copy; safe to read freely).</summary>
    Task<AppConfigOptions> GetEffectiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the latest version for "android" or "ios" (upsert). The caller has validated the format.</summary>
    Task SaveLatestVersionAsync(string platform, string version, CancellationToken cancellationToken = default);
}

public class AppVersionPolicyService : IAppVersionPolicyService
{
    private readonly AppDbContext _db;
    private readonly IOptionsMonitor<AppConfigOptions> _options;

    public AppVersionPolicyService(AppDbContext db, IOptionsMonitor<AppConfigOptions> options)
    {
        _db = db;
        _options = options;
    }

    public static string KeyFor(string platform) =>
        platform == "ios" ? "AppConfig.Ios.LatestVersion" : "AppConfig.Android.LatestVersion";

    public async Task<AppConfigOptions> GetEffectiveAsync(CancellationToken cancellationToken = default)
    {
        var baseConfig = _options.CurrentValue;
        var keys = new[] { KeyFor("android"), KeyFor("ios") };
        var saved = await _db.Configurations.AsNoTracking()
            .Where(c => keys.Contains(c.ConfigKey))
            .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue, cancellationToken);

        return new AppConfigOptions
        {
            Android = Merge(baseConfig.Android, saved.GetValueOrDefault(keys[0])),
            Ios = Merge(baseConfig.Ios, saved.GetValueOrDefault(keys[1]))
        };
    }

    public async Task SaveLatestVersionAsync(string platform, string version, CancellationToken cancellationToken = default)
    {
        var key = KeyFor(platform);
        var row = await _db.Configurations.FirstOrDefaultAsync(c => c.ConfigKey == key, cancellationToken);
        if (row == null)
        {
            _db.Configurations.Add(new ConfigurationEntry { ConfigKey = key, ConfigValue = version, CreatedOn = DateTime.Now });
        }
        else
        {
            row.ConfigValue = version;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static AppVersionPolicy Merge(AppVersionPolicy source, string? savedLatest) => new()
    {
        MinimumRequiredVersion = source.MinimumRequiredVersion,
        LatestVersion = string.IsNullOrWhiteSpace(savedLatest) ? source.LatestVersion : savedLatest.Trim(),
        ForceUpdate = source.ForceUpdate,
        StoreUrl = source.StoreUrl,
        UpdateMessage = source.UpdateMessage
    };
}
