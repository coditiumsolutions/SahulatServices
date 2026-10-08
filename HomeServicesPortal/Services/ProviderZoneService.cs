using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public interface IProviderZoneService
{
    /// <summary>Configured zone names (Configurations key=Zone).</summary>
    Task<List<string>> GetZoneOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>A provider's zones, alphabetical. Empty if none or provider not found.</summary>
    Task<List<string>> GetZonesAsync(int providerUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates requested names against the configured options (case-insensitive, canonical spelling returned,
    /// duplicates removed). A zone the provider already has is also accepted so an edit never fails on a zone
    /// an admin later removed from the config.
    /// </summary>
    Task<(List<string>? Zones, string? Error)> ResolveZonesAsync(
        IEnumerable<string>? requested, int? providerUid, CancellationToken cancellationToken = default);

    /// <summary>Full-replace of a provider's zones with already-resolved names; also refreshes Providers.Zone.</summary>
    Task SetZonesAsync(int providerUid, List<string> resolvedZones, CancellationToken cancellationToken = default);

    /// <summary>Resolve + set for a provider that already exists.</summary>
    Task<(bool Success, string? Error)> UpdateZonesAsync(
        int providerUid, IEnumerable<string>? requested, CancellationToken cancellationToken = default);
}

public class ProviderZoneService : IProviderZoneService
{
    private const string ZoneConfigKey = "Zone";
    private const int MaxZoneColumnLength = 100;

    /// <summary>Most zones one provider can work in.</summary>
    public const int MaxZonesPerProvider = 2;

    private readonly AppDbContext _db;
    private readonly IConfigurationEntryService _configurations;

    public ProviderZoneService(AppDbContext db, IConfigurationEntryService configurations)
    {
        _db = db;
        _configurations = configurations;
    }

    public async Task<List<string>> GetZoneOptionsAsync(CancellationToken cancellationToken = default)
        => (await _configurations.GetValuesByKeyAsync(ZoneConfigKey, cancellationToken)).ToList();

    public async Task<List<string>> GetZonesAsync(int providerUid, CancellationToken cancellationToken = default)
        => await _db.ProviderZones
            .AsNoTracking()
            .Where(z => z.ProviderUid == providerUid)
            .OrderBy(z => z.ZoneName)
            .Select(z => z.ZoneName)
            .ToListAsync(cancellationToken);

    public async Task<(List<string>? Zones, string? Error)> ResolveZonesAsync(
        IEnumerable<string>? requested, int? providerUid, CancellationToken cancellationToken = default)
    {
        var wanted = (requested ?? Enumerable.Empty<string>())
            .Where(z => !string.IsNullOrWhiteSpace(z))
            .Select(z => z.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wanted.Count == 0)
        {
            return (new List<string>(), null);
        }

        if (wanted.Count > MaxZonesPerProvider)
        {
            return (null, $"A provider can have at most {MaxZonesPerProvider} zones.");
        }

        var allowed = await GetZoneOptionsAsync(cancellationToken);
        if (providerUid.HasValue)
        {
            allowed.AddRange(await GetZonesAsync(providerUid.Value, cancellationToken));
        }

        var resolved = new List<string>();
        foreach (var name in wanted)
        {
            var match = allowed.FirstOrDefault(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                return (null, $"Zone '{name}' is not one of the configured zones.");
            }

            if (!resolved.Contains(match, StringComparer.OrdinalIgnoreCase))
            {
                resolved.Add(match);
            }
        }

        return (resolved, null);
    }

    public async Task SetZonesAsync(int providerUid, List<string> resolvedZones, CancellationToken cancellationToken = default)
    {
        var existing = await _db.ProviderZones
            .Where(z => z.ProviderUid == providerUid)
            .ToListAsync(cancellationToken);

        _db.ProviderZones.RemoveRange(existing.Where(e =>
            !resolvedZones.Contains(e.ZoneName, StringComparer.OrdinalIgnoreCase)));

        foreach (var name in resolvedZones)
        {
            if (!existing.Any(e => e.ZoneName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _db.ProviderZones.Add(new ProviderZone
                {
                    ProviderUid = providerUid,
                    ZoneName = name,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        var display = resolvedZones.Count == 0 ? null : string.Join(", ", resolvedZones.OrderBy(z => z));
        if (display is { Length: > MaxZoneColumnLength })
        {
            display = display[..MaxZoneColumnLength];
        }

        await _db.Providers
            .Where(p => p.Uid == providerUid)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Zone, display), cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> UpdateZonesAsync(
        int providerUid, IEnumerable<string>? requested, CancellationToken cancellationToken = default)
    {
        if (!await _db.Providers.AnyAsync(p => p.Uid == providerUid, cancellationToken))
        {
            return (false, "Provider not found.");
        }

        var (zones, error) = await ResolveZonesAsync(requested, providerUid, cancellationToken);
        if (zones == null)
        {
            return (false, error);
        }

        await SetZonesAsync(providerUid, zones, cancellationToken);
        return (true, null);
    }
}
