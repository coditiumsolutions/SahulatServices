using HomeServicesPortal.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace HomeServicesPortal.Services;

/// <summary>
/// Image optimization helper using ImageSharp.
/// Fixes orientation, optionally downscales, and encodes as JPEG when needed.
/// </summary>
public class ImageService : IImageService
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<ImageService> _logger;

    public ImageService(IOptions<FileStorageOptions> options, ILogger<ImageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task OptimizeAndSaveAsJpegAsync(
        Stream source,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        if (source.CanSeek)
        {
            source.Position = 0;
        }

        // Buffer so we can optionally copy the original JPEG without recompressing.
        await using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        using var image = await Image.LoadAsync(buffer, cancellationToken);
        var formatName = image.Metadata.DecodedImageFormat?.Name ?? string.Empty;
        var isJpeg = formatName.Equals("JPEG", StringComparison.OrdinalIgnoreCase)
                     || formatName.Equals("JPG", StringComparison.OrdinalIgnoreCase);

        var beforeWidth = image.Width;
        var beforeHeight = image.Height;
        image.Mutate(ctx => ctx.AutoOrient());
        var orientationChanged = image.Width != beforeWidth || image.Height != beforeHeight;

        var maxWidth = Math.Max(1, _options.MaxImageWidth);
        var quality = Math.Clamp(_options.JpegQuality, 1, 100);
        var needsResize = image.Width > maxWidth;

        if (needsResize)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(maxWidth, 0),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));

            _logger.LogInformation(
                "Resized image to {Width}x{Height} before saving to {Path}",
                image.Width,
                image.Height,
                destinationPath);
        }

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Skip recompression when the source is already a suitable JPEG.
        if (isJpeg && !needsResize && !orientationChanged)
        {
            buffer.Position = 0;
            await using var output = File.Create(destinationPath);
            await buffer.CopyToAsync(output, cancellationToken);
            _logger.LogInformation("Saved original JPEG without recompression to {Path}", destinationPath);
            return;
        }

        var encoder = new JpegEncoder { Quality = quality };
        await image.SaveAsJpegAsync(destinationPath, encoder, cancellationToken);
    }
}
