using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IProviderDetailService
{
    Task<(bool Success, string? Error, ProviderDetailApiDto? Data)> GetProviderDetailAsync(
        int providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ProviderDetailApiDto? Data)> UpdateProviderDetailAsync(
        UpdateProviderDetailRequestDto request,
        CancellationToken cancellationToken = default);
}
