using SkiaSharp;
using Xunit;

namespace Jellyfin.Drawing.Skia.Tests;

/// <summary>
/// Covers what <see cref="SkiaEncoder.ResizeImage"/> does either side of a resize: at matching
/// dimensions it must not touch the image at all, and sharpening belongs to downscales only.
/// </summary>
public class SkiaEncoderResizeTests
{
    private static SKBitmap CreateEdgeBitmap(int width, int height)
    {
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(40, 60, 80));
        using var paint = new SKPaint { Color = new SKColor(220, 210, 200) };
        canvas.DrawRect(SKRect.Create(0, 0, width / 2f, height), paint);

        return bitmap;
    }

    private static SKImageInfo InfoFor(SKBitmap source, int width, int height)
        => new SKImageInfo(width, height, source.ColorType, source.AlphaType, source.ColorSpace);

    /// <summary>
    /// Draws without sharpening, which is what the resize is expected to reduce to when it is not
    /// downscaling.
    /// </summary>
    private static SKBitmap DrawOnly(SKBitmap source, SKImageInfo targetInfo, SKSamplingOptions sampling)
    {
        var target = new SKBitmap(targetInfo);
        using var canvas = new SKCanvas(target);
        using var paint = new SKPaint();
        canvas.DrawBitmap(
            source,
            SKRect.Create(0, 0, source.Width, source.Height),
            SKRect.Create(0, 0, targetInfo.Width, targetInfo.Height),
            sampling,
            paint);

        return target;
    }

    private static void AssertSamePixels(SKBitmap expected, SKBitmap actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void ResizeImage_MatchingDimensions_ReturnsTheImageUntouched()
    {
        using var source = CreateEdgeBitmap(16, 16);

        using var result = SkiaEncoder.ResizeImage(source, InfoFor(source, 16, 16));
        using var resultBitmap = SKBitmap.FromImage(result);

        // Unsharpened, so the edge is still exactly where it was.
        AssertSamePixels(source, resultBitmap);
    }

    [Fact]
    public void ResizeImage_Upscale_DoesNotSharpen()
    {
        using var source = CreateEdgeBitmap(8, 8);
        var targetInfo = InfoFor(source, 24, 24);

        using var result = SkiaEncoder.ResizeImage(source, targetInfo);
        using var resultBitmap = SKBitmap.FromImage(result);
        using var expected = DrawOnly(source, targetInfo, SkiaEncoder.UpscaleSamplingOptions);

        AssertSamePixels(expected, resultBitmap);
    }

    [Fact]
    public void ResizeImage_Downscale_StillSharpens()
    {
        using var source = CreateEdgeBitmap(32, 32);
        var targetInfo = InfoFor(source, 16, 16);

        using var result = SkiaEncoder.ResizeImage(source, targetInfo);
        using var resultBitmap = SKBitmap.FromImage(result);
        using var unsharpened = DrawOnly(source, targetInfo, SkiaEncoder.DefaultSamplingOptions);
        using var sharpened = DrawOnly(source, targetInfo, SkiaEncoder.DefaultSamplingOptions);
        SkiaEncoder.SharpenInPlace(sharpened);

        AssertSamePixels(sharpened, resultBitmap);
        // Guards the test itself: the edge has to be something sharpening actually changes.
        Assert.NotEqual(unsharpened.GetPixel(8, 8), sharpened.GetPixel(8, 8));
    }
}
