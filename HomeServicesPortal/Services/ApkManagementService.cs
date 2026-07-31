using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Options;
using Microsoft.Extensions.Options;

namespace HomeServicesPortal.Services;

/// <summary>
/// Manages the single distributable Android APK served from the public download page.
/// Only one APK is kept at a time; uploading a new one replaces the previous file.
/// </summary>
public class ApkManagementService : IApkManagementService
{
    private const long MaxFileSizeBytes = 250 * 1024 * 1024;

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ApkManagementService> _logger;
    private readonly string _physicalRoot;

    public ApkManagementService(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment,
        ILogger<ApkManagementService> logger)
    {
        _environment = environment;
        _logger = logger;

        var configured = (options.Value.ApkDownloads ?? "wwwroot/downloads").Trim().Replace('\\', '/').Trim('/');
        var underWwwRoot = configured.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase)
            ? configured["wwwroot/".Length..]
            : configured;

        _physicalRoot = Path.GetFullPath(Path.Combine(
            _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
            underWwwRoot));

        Directory.CreateDirectory(_physicalRoot);
    }

    public ApkFileInfo? GetCurrentApk()
    {
        var file = Directory.EnumerateFiles(_physicalRoot, "*.apk").FirstOrDefault();
        if (file == null)
        {
            return null;
        }

        var info = new FileInfo(file);
        return new ApkFileInfo(info.Name, info.Length, info.LastWriteTimeUtc);
    }

    public async Task<(bool Success, string? Error)> UploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length <= 0)
        {
            return (false, "Please choose an APK file to upload.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (false, $"File exceeds the maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLowerInvariant();
        if (extension != "apk")
        {
            return (false, "Only .apk files are allowed.");
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var destinationPath = Path.GetFullPath(Path.Combine(_physicalRoot, safeFileName));
        if (!destinationPath.StartsWith(_physicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Invalid file name.");
        }

        // Only one APK is kept at a time — clear any existing ones before saving the new file.
        foreach (var existing in Directory.EnumerateFiles(_physicalRoot, "*.apk"))
        {
            File.Delete(existing);
        }

        try
        {
            await using var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            await file.CopyToAsync(stream, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save uploaded APK {FileName}", safeFileName);
            return (false, "Failed to save the uploaded file.");
        }

        _logger.LogInformation("Uploaded APK {FileName} ({Bytes} bytes)", safeFileName, file.Length);
        return (true, null);
    }

    public (bool Success, string? Error) Delete(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var path = Path.GetFullPath(Path.Combine(_physicalRoot, safeFileName));
        if (!path.StartsWith(_physicalRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            return (false, "File not found.");
        }

        File.Delete(path);
        _logger.LogInformation("Deleted APK {FileName}", safeFileName);
        return (true, null);
    }

    public string? GetPhysicalPath(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        var path = Path.GetFullPath(Path.Combine(_physicalRoot, safeFileName));
        if (!path.StartsWith(_physicalRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
        {
            return null;
        }

        return path;
    }
}
