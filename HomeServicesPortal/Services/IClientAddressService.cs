using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IClientAddressService
{
    Task<IReadOnlyList<ClientAddressApiDto>> GetAddressesAsync(
        int? clientUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ClientAddressApiDto? Data)> GetAddressByIdAsync(
        int addressUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ClientAddressApiDto? Data)> CreateAddressAsync(
        CreateClientAddressRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ClientAddressApiDto? Data)> UpdateAddressAsync(
        UpdateClientAddressRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAddressAsync(
        int addressUid,
        CancellationToken cancellationToken = default);
}
