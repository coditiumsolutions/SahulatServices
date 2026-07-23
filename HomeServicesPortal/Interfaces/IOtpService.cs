using HomeServicesPortal.DTOs;

namespace HomeServicesPortal.Interfaces;

public interface IOtpService
{
    Task<(bool Success, string? Error, SendOtpResponse? Data, int StatusCode)> SendOtpAsync(
        SendOtpRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, VerifyOtpResponse? Data, int StatusCode)> VerifyOtpAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, SendOtpResponse? Data, int StatusCode)> ResendOtpAsync(
        ResendOtpRequest request,
        CancellationToken cancellationToken = default);
}
