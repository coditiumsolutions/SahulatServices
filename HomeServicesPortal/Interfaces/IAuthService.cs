using HomeServicesPortal.DTOs;

namespace HomeServicesPortal.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, RegistrationResponse? Data)> RegisterClientAsync(
        RegisterClientRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ProviderUpgradeResponse? Data, int StatusCode)> RegisterProviderAsync(
        RegisterProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, RegistrationResponse? Data)> RegisterStaffAsync(
        RegisterStaffRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, LoginResponse? Data, int StatusCode)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, DeleteAccountResponse? Data, int StatusCode)> DeleteAccountAsync(
        DeleteAccountRequest request,
        CancellationToken cancellationToken = default);
}
