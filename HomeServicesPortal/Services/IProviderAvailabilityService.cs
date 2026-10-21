using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IProviderAvailabilityService
{
    Task<(bool Success, string? Error, ProviderAvailableStatusApiDto? Data)> GetProviderAvailabilityStatusAsync(
        int providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ProviderAvailableStatusApiDto? Data)> SaveProviderAvailabilityStatusAsync(
        SetProviderAvailableStatusRequestDto request,
        CancellationToken cancellationToken = default);
}
