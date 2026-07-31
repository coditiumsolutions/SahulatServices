namespace HomeServicesPortal.Interfaces;

public record ApkFileInfo(string FileName, long SizeBytes, DateTime LastModifiedUtc);

public interface IApkManagementService
{
    ApkFileInfo? GetCurrentApk();
    Task<(bool Success, string? Error)> UploadAsync(IFormFile file, CancellationToken cancellationToken = default);
    (bool Success, string? Error) Delete(string fileName);
    string? GetPhysicalPath(string fileName);
}
