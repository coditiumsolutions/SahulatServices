using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IClientDetailService
{
    Task<(bool Success, string? Error, ClientDetailApiDto? Data)> GetClientDetailAsync(
        int clientUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ClientDetailApiDto? Data)> UpdateClientDetailAsync(
        UpdateClientDetailRequestDto request,
        CancellationToken cancellationToken = default);
}
