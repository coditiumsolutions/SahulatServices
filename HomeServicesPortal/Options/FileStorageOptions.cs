namespace HomeServicesPortal.Options;

/// <summary>
/// Filesystem storage and image optimization settings for provider documents.
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Root folder for provider document uploads, relative to content root.
    /// Example: wwwroot/uploads/providers
    /// </summary>
    public string ProviderDocuments { get; set; } = "wwwroot/uploads/providers";

    /// <summary>
    /// Folder where the distributable Android APK is stored, relative to content root.
    /// Example: wwwroot/downloads
    /// </summary>
    public string ApkDownloads { get; set; } = "wwwroot/downloads";

    /// <summary>Maximum allowed size per uploaded image in bytes (default 5 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Maximum image width in pixels. Larger images are resized (aspect ratio preserved).</summary>
    public int MaxImageWidth { get; set; } = 1600;

    /// <summary>JPEG encoder quality (1–100) used when optimizing or converting images.</summary>
    public int JpegQuality { get; set; } = 80;

    /// <summary>Allowed file extensions (lowercase, without dot).</summary>
    public string[] AllowedExtensions { get; set; } = ["jpg", "jpeg", "png"];

    /// <summary>Allowed MIME content types.</summary>
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/jpg",
        "image/pjpeg",
        "image/png"
    ];
}
