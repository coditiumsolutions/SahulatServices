using Microsoft.AspNetCore.Http;

namespace HomeServicesPortal.Services;

/// <summary>Safe filesystem operations for provider document uploads under wwwroot.</summary>
public interface IFileStorageService
{
    /// <summary>
    /// Validates extension/MIME/size, optimizes the image, and saves it as a fixed file name
    /// under uploads/providers/{providerUid}/. Returns the relative path for SQL storage.
    /// </summary>
    Task<(bool Success, string? Error, string? RelativePath, int StatusCode)> SaveProviderImageAsync(
        int providerUid,
        IFormFile file,
        string fixedFileName,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes known provider document files and removes the folder if empty.</summary>
    void DeleteProviderDocumentFiles(int providerUid);

    /// <summary>Builds the relative DB path for a fixed file name (forward slashes).</summary>
    string BuildRelativePath(int providerUid, string fixedFileName);
}
