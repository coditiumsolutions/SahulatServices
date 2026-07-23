namespace HomeServicesPortal.Services;

/// <summary>Server-side image validation and optimization for uploads.</summary>
public interface IImageService
{
    /// <summary>
    /// Validates and optionally optimizes the image stream, then writes a JPEG to <paramref name="destinationPath"/>.
    /// Does not upscale. Resizes when width exceeds the configured maximum. Applies EXIF orientation.
    /// </summary>
    Task OptimizeAndSaveAsJpegAsync(
        Stream source,
        string destinationPath,
        CancellationToken cancellationToken = default);
}
