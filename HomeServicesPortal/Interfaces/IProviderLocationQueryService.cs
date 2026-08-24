using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Interfaces;

public interface IProviderLocationQueryService
{
    Task<IReadOnlyList<NearbyProviderApiDto>> FindNearbyProvidersAsync(
        NearbyProvidersRequestDto request, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, double? DistanceKm)> GetDistanceAsync(
        int clientAddressUid, int providerUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent location write shared by the SignalR hub and the REST fallback endpoint.
    /// Applies the update only if clientTimestampUtc is strictly newer than the currently stored
    /// LocationUpdatedOn, so duplicate/out-of-order calls are safe no-ops rather than clobbers.
    /// AppliedAtUtc is null when the call was a no-op (nothing newer to apply).
    /// </summary>
    Task<(bool Success, string? Error, DateTime? AppliedAtUtc)> UpdateCurrentLocationAsync(
        int providerUid, decimal latitude, decimal longitude, DateTime clientTimestampUtc,
        CancellationToken cancellationToken = default);
}
