using HomeServicesPortal.Models.Api.Auth;

namespace HomeServicesPortal.Services.Auth;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponseDto? Data)> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, RegisterCustomerResponseDto? Data)> RegisterCustomerAsync(
        RegisterCustomerRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, RegisterProviderResponseDto? Data)> RegisterProviderAsync(
        RegisterProviderRequestDto request,
        CancellationToken cancellationToken = default);
}
