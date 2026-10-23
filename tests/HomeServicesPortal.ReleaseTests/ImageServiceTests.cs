using HomeServicesPortal.Options;
using HomeServicesPortal.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkiaSharp;
using Xunit;

namespace HomeServicesPortal.ReleaseTests;

public class ImageServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "img-" + Guid.NewGuid().ToString("N"));

    public ImageServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static ImageService Create(int maxWidth = 1600) =>
        new(Microsoft.Extensions.Options.Options.Create(new FileStorageOptions { MaxImageWidth = maxWidth, JpegQuality = 80 }),
            NullLogger<ImageService>.Instance);

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format)
    {
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(format, 90).ToArray();
    }

    private static SKBitmap Solid(int w, int h, SKColor color)
    {
        var bmp = new SKBitmap(w, h);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(color);
        return bmp;
    }

    private async Task<(SKBitmap Result, byte[] Bytes)> RunAsync(byte[] input, int maxWidth = 1600)
    {
        var path = Path.Combine(_dir, "out.jpg");
        await Create(maxWidth).OptimizeAndSaveAsJpegAsync(new MemoryStream(input), path);
        var bytes = await File.ReadAllBytesAsync(path);
        return (SKBitmap.Decode(bytes), bytes);
    }

    [Fact]
    public async Task Small_jpeg_is_copied_without_recompression()
    {
        using var src = Solid(200, 100, SKColors.Red);
        var input = Encode(src, SKEncodedImageFormat.Jpeg);

        var (result, bytes) = await RunAsync(input);
        using (result)
        {
            Assert.Equal(input, bytes);
        }
    }

    [Fact]
    public async Task Wide_png_is_resized_and_saved_as_jpeg()
    {
        using var src = Solid(3200, 1600, SKColors.Blue);

        var (result, bytes) = await RunAsync(Encode(src, SKEncodedImageFormat.Png), maxWidth: 1000);
        using (result)
        {
            Assert.Equal(1000, result.Width);
            Assert.Equal(500, result.Height);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xD8, bytes[1]);
        }
    }

    [Fact]
    public async Task Transparent_png_is_flattened_onto_white()
    {
        using var src = Solid(50, 50, SKColors.Transparent);

        var (result, _) = await RunAsync(Encode(src, SKEncodedImageFormat.Png));
        using (result)
        {
            var px = result.GetPixel(25, 25);
            Assert.True(px.Red > 240 && px.Green > 240 && px.Blue > 240);
        }
    }

    [Fact]
    public async Task Exif_orientation_is_applied()
    {
        // 80x40 image: left half red, right half blue. Orientation 6 (rotate 90 CW) -> 40x80, red on top.
        using var src = new SKBitmap(80, 40);
        using (var canvas = new SKCanvas(src))
        {
            canvas.Clear(SKColors.Blue);
            using var red = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(0, 0, 40, 40, red);
        }

        var input = WithOrientation(Encode(src, SKEncodedImageFormat.Jpeg), 6);

        var (result, _) = await RunAsync(input);
        using (result)
        {
            Assert.Equal(40, result.Width);
            Assert.Equal(80, result.Height);
            var top = result.GetPixel(20, 10);
            var bottom = result.GetPixel(20, 70);
            Assert.True(top.Red > 200 && top.Blue < 80, $"top was {top}");
            Assert.True(bottom.Blue > 200 && bottom.Red < 80, $"bottom was {bottom}");
        }
    }

    [Fact]
    public async Task Non_image_bytes_are_rejected()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync("not an image"u8.ToArray()));
    }

    [Fact]
    public async Task Unsupported_format_is_rejected()
    {
        using var src = Solid(20, 20, SKColors.Green);
        await Assert.ThrowsAsync<InvalidDataException>(() => RunAsync(Encode(src, SKEncodedImageFormat.Webp)));
    }

    /// <summary>Inserts a minimal EXIF APP1 segment carrying only the orientation tag right after SOI.</summary>
    private static byte[] WithOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,                         // APP1, length 34
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,             // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // little-endian TIFF header, IFD at 8
            0x01, 0x00,                                     // one entry
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, // tag 0x0112, SHORT, count 1
            (byte)(orientation & 0xFF), (byte)(orientation >> 8), 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,                         // no next IFD
        ];

        var result = new byte[jpeg.Length + exif.Length];
        result[0] = jpeg[0];
        result[1] = jpeg[1];
        Array.Copy(exif, 0, result, 2, exif.Length);
        Array.Copy(jpeg, 2, result, 2 + exif.Length, jpeg.Length - 2);
        return result;
    }
}
