using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

/// <summary>
/// Nearby-provider search (Haversine, computed in C# per the project's $0-cost strategy — not
/// SQL spatial types) and the idempotent provider live-location write shared by the SignalR hub
/// and the REST location-push fallback.
/// </summary>
public class ProviderLocationQueryService : IProviderLocationQueryService
{
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;

    public ProviderLocationQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<NearbyProviderApiDto>> FindNearbyProvidersAsync(
        NearbyProvidersRequestDto request, CancellationToken cancellationToken = default)
    {
        var query = _db.Providers
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Latitude != null && p.Longitude != null);

        if (request.OnlyAvailable)
        {
            query = query.Where(p => p.IsAvailable);
        }

        if (request.OnlyVerified)
        {
            query = query.Where(p => p.IsVerified);
        }

        if (request.CategoryUid.HasValue)
        {
            query = query.Where(p => p.CategoryUid == request.CategoryUid.Value);
        }

        // Candidate set pulled into memory — Haversine trig can't be translated to SQL, and the
        // spec calls for a C# calculation rather than SQL spatial queries. Fine at current provider
        // scale; a cheap SQL bounding-box pre-filter (Latitude/Longitude BETWEEN...) would be the
        // next optimization if the Providers table grows large.
        var candidates = await query.ToListAsync(cancellationToken);

        var results = candidates
            .Select(p => new NearbyProviderApiDto
            {
                Uid = p.Uid,
                FullName = p.FullName,
                CategoryUid = p.CategoryUid,
                CategoryName = p.Category?.CategoryName ?? string.Empty,
                AverageRating = p.AverageRating,
                IsAvailable = p.IsAvailable,
                IsVerified = p.IsVerified,
                Latitude = p.Latitude!.Value,
                Longitude = p.Longitude!.Value,
                DistanceKm = DistanceHelper.HaversineDistanceKm(request.Lat, request.Lng, p.Latitude!.Value, p.Longitude!.Value)
            })
            .Where(p => p.DistanceKm <= request.RadiusKm)
            .OrderBy(p => p.DistanceKm)
            .Take(request.MaxResults)
            .ToList();

        return results;
    }

    public async Task<(bool Success, string? Error, double? DistanceKm)> GetDistanceAsync(
        int clientAddressUid, int providerUid, CancellationToken cancellationToken = default)
    {
        var address = await _db.ClientAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Uid == clientAddressUid, cancellationToken);

        if (address == null)
        {
            return (false, "Client address not found.", null);
        }

        if (address.Latitude == null || address.Longitude == null)
        {
            return (false, "Client address does not have a location set.", null);
        }

        var provider = await _db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Uid == providerUid, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.", null);
        }

        if (provider.Latitude == null || provider.Longitude == null)
        {
            return (false, "Provider does not have a current location.", null);
        }

        var distanceKm = DistanceHelper.HaversineDistanceKm(
            address.Latitude.Value, address.Longitude.Value,
            provider.Latitude.Value, provider.Longitude.Value);

        return (true, null, distanceKm);
    }

    public async Task<(bool Success, string? Error, DateTime? AppliedAtUtc)> UpdateCurrentLocationAsync(
        int providerUid, decimal latitude, decimal longitude, DateTime clientTimestampUtc,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        if (clientTimestampUtc > nowUtc + MaxClockSkew || clientTimestampUtc < nowUtc - MaxClockSkew)
        {
            return (false, "clientTimestampUtc is too far from server time.", null);
        }

        var exists = await _db.Providers.AsNoTracking().AnyAsync(p => p.Uid == providerUid, cancellationToken);
        if (!exists)
        {
            return (false, "Provider not found.", null);
        }

        // Conditional UPDATE — only applies when clientTimestampUtc is strictly newer than what's
        // stored, so a duplicate retry or an out-of-order/delayed call is a safe no-op instead of
        // clobbering a fresher position written via the other transport (Hub vs REST fallback).
        var rows = await _db.Providers
            .Where(p => p.Uid == providerUid && (p.LocationUpdatedOn == null || p.LocationUpdatedOn < clientTimestampUtc))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Latitude, latitude)
                .SetProperty(p => p.Longitude, longitude)
                .SetProperty(p => p.LocationUpdatedOn, clientTimestampUtc), cancellationToken);

        return rows > 0 ? (true, null, clientTimestampUtc) : (true, null, null);
    }
}
