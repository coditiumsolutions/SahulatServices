using HomeServicesPortal.Options;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace HomeServicesPortal.Services;

/// <summary>
/// Image optimization helper using SkiaSharp.
/// Fixes orientation, optionally downscales, and encodes as JPEG when needed.
/// </summary>
public class ImageService : IImageService
{
    // Guards against decompression bombs: a small file can declare a huge canvas.
    private const long MaxPixels = 64_000_000;

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

        using var codec = SKCodec.Create(buffer)
            ?? throw new InvalidDataException("File content is not a valid image.");

        var isJpeg = codec.EncodedFormat == SKEncodedImageFormat.Jpeg;
        if (!isJpeg && codec.EncodedFormat != SKEncodedImageFormat.Png)
        {
            throw new InvalidDataException("File content is not a valid image.");
        }

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaxPixels)
        {
            throw new InvalidDataException("Image dimensions are not supported.");
        }

        using var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("File content is not a valid image.");

        var origin = codec.EncodedOrigin;
        var orientationChanged = origin != SKEncodedOrigin.TopLeft;
        using var oriented = orientationChanged ? ApplyOrigin(decoded, origin) : null;
        var current = oriented ?? decoded;

        var maxWidth = Math.Max(1, _options.MaxImageWidth);
        var quality = Math.Clamp(_options.JpegQuality, 1, 100);
        var needsResize = current.Width > maxWidth;

        SKBitmap? resized = null;
        try
        {
            if (needsResize)
            {
                var height = Math.Max(1, (int)Math.Round(current.Height * (double)maxWidth / current.Width));
                resized = current.Resize(
                    new SKImageInfo(maxWidth, height, SKColorType.Rgba8888, SKAlphaType.Premul),
                    new SKSamplingOptions(SKCubicResampler.Mitchell));
                if (resized == null)
                {
                    throw new InvalidOperationException("Failed to resize image.");
                }

                current = resized;
                _logger.LogInformation(
                    "Resized image to {Width}x{Height} before saving to {Path}",
                    current.Width,
                    current.Height,
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

            // JPEG has no alpha: flatten onto white so transparent PNG areas do not turn black.
            using var flat = new SKBitmap(current.Width, current.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using (var canvas = new SKCanvas(flat))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(current, 0, 0);
            }

            using var image = SKImage.FromBitmap(flat);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality)
                ?? throw new InvalidOperationException("Failed to encode JPEG.");
            await using var file = File.Create(destinationPath);
            data.SaveTo(file);
        }
        finally
        {
            resized?.Dispose();
        }
    }

    /// <summary>Redraws the bitmap upright according to its EXIF orientation.</summary>
    private static SKBitmap ApplyOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        float w = bitmap.Width, h = bitmap.Height;
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        // Each matrix maps a source pixel to its upright position.
        var m = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix { ScaleX = -1, TransX = w, ScaleY = 1, Persp2 = 1 },
            SKEncodedOrigin.BottomRight => new SKMatrix { ScaleX = -1, TransX = w, ScaleY = -1, TransY = h, Persp2 = 1 },
            SKEncodedOrigin.BottomLeft => new SKMatrix { ScaleX = 1, ScaleY = -1, TransY = h, Persp2 = 1 },
            SKEncodedOrigin.LeftTop => new SKMatrix { SkewX = 1, SkewY = 1, Persp2 = 1 },
            SKEncodedOrigin.RightTop => new SKMatrix { SkewX = -1, TransX = h, SkewY = 1, Persp2 = 1 },
            SKEncodedOrigin.RightBottom => new SKMatrix { SkewX = -1, TransX = h, SkewY = -1, TransY = w, Persp2 = 1 },
            SKEncodedOrigin.LeftBottom => new SKMatrix { SkewX = 1, SkewY = -1, TransY = w, Persp2 = 1 },
            _ => SKMatrix.Identity,
        };

        var result = new SKBitmap(
            swap ? bitmap.Height : bitmap.Width,
            swap ? bitmap.Width : bitmap.Height,
            bitmap.ColorType,
            bitmap.AlphaType);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(m);
        canvas.DrawBitmap(bitmap, 0, 0);
        return result;
    }
}
