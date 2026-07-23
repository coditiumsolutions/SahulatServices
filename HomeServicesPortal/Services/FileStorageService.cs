using HomeServicesPortal.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace HomeServicesPortal.Services;

/// <summary>
/// Saves provider images under a configured wwwroot-relative folder and returns relative SQL paths.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> FixedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "profile.jpg",
        "cnic_front.jpg",
        "cnic_back.jpg"
    };

    private readonly FileStorageOptions _options;
    private readonly IImageService _imageService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _physicalRoot;
    private readonly string _relativeRoot;

    public FileStorageService(
        IOptions<FileStorageOptions> options,
        IImageService imageService,
        IWebHostEnvironment environment,
        ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _imageService = imageService;
        _environment = environment;
        _logger = logger;

        (_physicalRoot, _relativeRoot) = ResolveRoots(_options.ProviderDocuments, _environment);
    }

    public async Task<(bool Success, string? Error, string? RelativePath, int StatusCode)> SaveProviderImageAsync(
        int providerUid,
        IFormFile file,
        string fixedFileName,
        CancellationToken cancellationToken = default)
    {
        if (providerUid <= 0)
        {
            return (false, "ProviderUID must be a positive integer.", null, StatusCodes.Status400BadRequest);
        }

        if (!FixedFileNames.Contains(fixedFileName))
        {
            return (false, "Invalid destination file name.", null, StatusCodes.Status400BadRequest);
        }

        if (file == null || file.Length <= 0)
        {
            return (false, $"{fixedFileName} is required.", null, StatusCodes.Status400BadRequest);
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / (1024d * 1024d);
            return (false, $"File exceeds the maximum size of {maxMb:0.#} MB.", null, StatusCodes.Status400BadRequest);
        }

        var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Only jpg, jpeg, and png images are allowed.", null, StatusCodes.Status415UnsupportedMediaType);
        }

        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(contentType) ||
            !_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Unsupported image MIME type. Allowed: image/jpeg, image/png.", null, StatusCodes.Status415UnsupportedMediaType);
        }

        var providerFolder = GetSafeProviderFolder(providerUid);
        Directory.CreateDirectory(providerFolder);

        var destinationPath = Path.Combine(providerFolder, fixedFileName);
        destinationPath = Path.GetFullPath(destinationPath);
        if (!destinationPath.StartsWith(_physicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked path traversal attempt for provider {ProviderUid}: {Path}", providerUid, destinationPath);
            return (false, "Invalid upload path.", null, StatusCodes.Status400BadRequest);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _imageService.OptimizeAndSaveAsJpegAsync(stream, destinationPath, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "File content is not a valid image.", null, StatusCodes.Status415UnsupportedMediaType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save provider image {File} for provider {ProviderUid}", fixedFileName, providerUid);
            return (false, "Failed to process and save the uploaded image.", null, StatusCodes.Status500InternalServerError);
        }

        var relativePath = BuildRelativePath(providerUid, fixedFileName);
        _logger.LogInformation(
            "Saved provider document {RelativePath} ({Bytes} bytes uploaded)",
            relativePath,
            file.Length);

        return (true, null, relativePath, StatusCodes.Status200OK);
    }

    public void DeleteProviderDocumentFiles(int providerUid)
    {
        if (providerUid <= 0)
        {
            return;
        }

        var providerFolder = GetSafeProviderFolder(providerUid);
        if (!Directory.Exists(providerFolder))
        {
            return;
        }

        foreach (var fileName in FixedFileNames)
        {
            var path = Path.Combine(providerFolder, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogInformation("Deleted provider document file {Path}", path);
            }
        }

        if (Directory.Exists(providerFolder) && !Directory.EnumerateFileSystemEntries(providerFolder).Any())
        {
            Directory.Delete(providerFolder, recursive: false);
            _logger.LogInformation("Deleted empty provider folder {Folder}", providerFolder);
        }
    }

    public string BuildRelativePath(int providerUid, string fixedFileName)
    {
        return $"{_relativeRoot}/{providerUid}/{fixedFileName}".Replace('\\', '/');
    }

    private string GetSafeProviderFolder(int providerUid)
    {
        var folder = Path.GetFullPath(Path.Combine(_physicalRoot, providerUid.ToString()));
        if (!folder.StartsWith(_physicalRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid provider folder path.");
        }

        return folder;
    }

    private static (string PhysicalRoot, string RelativeRoot) ResolveRoots(
        string configuredPath,
        IWebHostEnvironment environment)
    {
        var normalized = (configuredPath ?? string.Empty).Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "wwwroot/uploads/providers";
        }

        string physical;
        string relative;

        if (normalized.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
        {
            var underWwwRoot = normalized["wwwroot/".Length..];
            physical = Path.GetFullPath(Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), underWwwRoot));
            relative = underWwwRoot;
        }
        else
        {
            physical = Path.GetFullPath(Path.Combine(environment.ContentRootPath, normalized));
            relative = normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"uploads/{normalized.TrimStart('/')}";
        }

        return (physical, relative.Replace('\\', '/').Trim('/'));
    }
}
