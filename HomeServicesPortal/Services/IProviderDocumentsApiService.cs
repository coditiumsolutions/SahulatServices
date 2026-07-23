using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

/// <summary>External API operations for provider profile / CNIC document management.</summary>
public interface IProviderDocumentsApiService
{
    Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> UploadDocumentsAsync(
        UploadProviderDocumentsRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> GetDocumentsAsync(
        int providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, int StatusCode)> DeleteDocumentsAsync(
        int providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ProviderDocumentsApiDto? Data, int StatusCode)> VerifyDocumentsAsync(
        VerifyProviderDocumentsRequestDto request,
        CancellationToken cancellationToken = default);
}
