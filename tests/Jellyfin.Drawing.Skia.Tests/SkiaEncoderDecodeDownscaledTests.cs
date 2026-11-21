using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Drawing.Skia.Tests;

public sealed class SkiaEncoderDecodeDownscaledTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "jellyfin-skia-" + Guid.NewGuid().ToString("N"));
    private readonly string _tempDirectory;
    private readonly SkiaEncoder _encoder;

    public SkiaEncoderDecodeDownscaledTests()
    {
        _tempDirectory = Path.Combine(_directory, "temp");
        Directory.CreateDirectory(_tempDirectory);
        _encoder = new(NullLogger<SkiaEncoder>.Instance, Mock.Of<IApplicationPaths>(p => p.TempDirectory == _tempDirectory));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Theory]
    [InlineData(2000, 3000, 150, 250, 375)] // 1/8
    [InlineData(2000, 3000, 300, 500, 750)] // 1/4
    [InlineData(1920, 1080, 600, 960, 540)] // 1/2, 1/4 would be less than 1.5x the output
    public void DecodeDownscaled_Jpeg_DecodesSmallestScaleCoveringOutput(int width, int height, int maxWidth, int decodedWidth, int decodedHeight)
    {
        using var source = CreateTestPattern(width, height);
        var input = WriteImage(source, SKEncodedImageFormat.Jpeg);

        using var bitmap = _encoder.DecodeDownscaled(input, false, new ImageProcessingOptions { MaxWidth = maxWidth }, out var originalSize);

        Assert.NotNull(bitmap);
        Assert.Equal(decodedWidth, bitmap.Width);
        Assert.Equal(decodedHeight, bitmap.Height);
        Assert.Equal(new ImageDimensions(width, height), originalSize);
    }

    [Fact]
    public void DecodeDownscaled_OnlyFullSizeCoversOutput_ReturnsNull()
    {
        // Half of 2000 is 1000, less than 1.5x the requested 700.
        using var source = CreateTestPattern(2000, 3000);
        var input = WriteImage(source, SKEncodedImageFormat.Jpeg);

        using var bitmap = _encoder.DecodeDownscaled(input, false, new ImageProcessingOptions { MaxWidth = 700 }, out var originalSize);

        Assert.Null(bitmap);
        Assert.Null(originalSize);
    }

    [Fact]
    public void DecodeDownscaled_NonLatinPath_RemovesTemporaryCopy()
    {
        using var source = CreateTestPattern(2000, 3000);
        var input = Path.Combine(_directory, "ポスター.jpg");
        File.Move(WriteImage(source, SKEncodedImageFormat.Jpeg), input);

        using var bitmap = _encoder.DecodeDownscaled(input, false, new ImageProcessingOptions { MaxWidth = 300 }, out _);

        Assert.NotNull(bitmap);
        Assert.Equal(500, bitmap.Width);
        Assert.Empty(Directory.GetFiles(_tempDirectory));
    }

    [Fact]
    public void DecodeDownscaled_Png_ReturnsNull()
    {
        using var source = CreateTestPattern(2000, 3000);
        var input = WriteImage(source, SKEncodedImageFormat.Png);

        using var bitmap = _encoder.DecodeDownscaled(input, false, new ImageProcessingOptions { MaxWidth = 300 }, out _);

        Assert.Null(bitmap);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, 2000, 3000, 300, 450)]
    [InlineData(SKEncodedImageFormat.Jpeg, 1000, 1500, 900, 1350)]
    [InlineData(SKEncodedImageFormat.Png, 2000, 3000, 300, 450)]
    public void EncodeImage_ProducesRequestedSize(SKEncodedImageFormat format, int width, int height, int maxWidth, int expectedHeight)
    {
        using var source = CreateTestPattern(width, height);
        var input = WriteImage(source, format);

        using var output = Encode(input, new ImageProcessingOptions { MaxWidth = maxWidth }, false);

        Assert.Equal(maxWidth, output.Width);
        Assert.Equal(expectedHeight, output.Height);
    }

    [Fact]
    public void EncodeImage_DownscaledJpeg_MatchesFullDecode()
    {
        using var source = CreateTestPattern(2000, 3000);
        var input = WriteImage(source, SKEncodedImageFormat.Jpeg);

        using var output = Encode(input, new ImageProcessingOptions { MaxWidth = 300 }, false);

        using var fullDecode = SKBitmap.Decode(input);
        using var expected = SkiaEncoder.ResizeImage(fullDecode, new SKImageInfo(300, 450, fullDecode.ColorType, fullDecode.AlphaType, fullDecode.ColorSpace));
        var difference = MeanAbsoluteDifference(expected, output);
        Assert.True(difference < 2, $"Mean absolute difference per channel was {difference}");
    }

    [Fact]
    public void EncodeImage_GrayscaleJpeg_KeepsTone()
    {
        using var source = new SKBitmap(new SKImageInfo(1000, 1000, SKColorType.Gray8, SKAlphaType.Opaque));
        source.Erase(new SKColor(90, 90, 90));
        var input = WriteImage(source, SKEncodedImageFormat.Jpeg);

        using var output = Encode(input, new ImageProcessingOptions { MaxWidth = 100 }, false);

        Assert.Equal(100, output.Width);
        Assert.Equal(100, output.Height);
        AssertColor(new SKColor(90, 90, 90), output.GetPixel(50, 50));
    }

    [Fact]
    public void EncodeImage_AutoOrient_AppliesExifRotation()
    {
        var input = WriteRedLeftBlueRightJpeg(ExifOrientation(SKEncodedOrigin.RightTop));

        using var output = Encode(input, new ImageProcessingOptions { MaxWidth = 100 }, true);

        // Rotated 90 degrees clockwise: the left half of the stored image ends up on top.
        Assert.Equal(100, output.Width);
        Assert.Equal(200, output.Height);
        AssertColor(SKColors.Red, output.GetPixel(50, 30));
        AssertColor(SKColors.Blue, output.GetPixel(50, 170));
    }

    [Fact]
    public void EncodeImage_NoAutoOrient_IgnoresExifRotation()
    {
        var input = WriteRedLeftBlueRightJpeg(ExifOrientation(SKEncodedOrigin.RightTop));

        using var output = Encode(input, new ImageProcessingOptions { MaxWidth = 100 }, false);

        Assert.Equal(100, output.Width);
        Assert.Equal(50, output.Height);
        AssertColor(SKColors.Red, output.GetPixel(15, 25));
        AssertColor(SKColors.Blue, output.GetPixel(85, 25));
    }

    private SKBitmap Encode(string input, ImageProcessingOptions options, bool autoOrient)
    {
        // Lossless output, so the tests see the resized pixels.
        var outputPath = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".png");
        var result = _encoder.EncodeImage(input, DateTime.UtcNow, outputPath, autoOrient, null, 90, options, ImageFormat.Png);
        Assert.Equal(outputPath, result);
        return SKBitmap.Decode(outputPath);
    }

    private string WriteImage(SKBitmap bitmap, SKEncodedImageFormat format, byte[]? exif = null)
    {
        using var data = bitmap.Encode(format, 95);
        var bytes = data.ToArray();
        if (exif is not null)
        {
            // The APP1 segment goes right after the SOI marker.
            bytes = [.. bytes.AsSpan(0, 2), .. exif, .. bytes.AsSpan(2)];
        }

        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + (format == SKEncodedImageFormat.Png ? ".png" : ".jpg"));
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string WriteRedLeftBlueRightJpeg(byte[] exif)
    {
        using var source = new SKBitmap(800, 400);
        using var canvas = new SKCanvas(source);
        canvas.Clear(SKColors.Blue);
        using var red = new SKPaint { Color = SKColors.Red };
        canvas.DrawRect(0, 0, 400, 400, red);
        return WriteImage(source, SKEncodedImageFormat.Jpeg, exif);
    }

    private static SKBitmap CreateTestPattern(int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            [SKColors.DarkBlue, SKColors.Orange, SKColors.White],
            SKShaderTileMode.Clamp);
        using var gradient = new SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, gradient);
        using var black = new SKPaint { Color = SKColors.Black };
        canvas.DrawRect(width / 4f, height / 4f, width / 2f, height / 2f, black);
        return bitmap;
    }

    /// <summary>
    /// Builds a minimal big-endian EXIF APP1 segment holding only the orientation tag.
    /// </summary>
    private static byte[] ExifOrientation(SKEncodedOrigin origin) =>
    [
        0xFF, 0xE1, 0x00, 0x22, // APP1, length 34
        (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
        (byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, // TIFF header, IFD0 at offset 8
        0x00, 0x01, // one entry
        0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, (byte)origin, 0x00, 0x00, // orientation, SHORT
        0x00, 0x00, 0x00, 0x00 // no next IFD
    ];

    private static double MeanAbsoluteDifference(SKBitmap expected, SKBitmap actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        long total = 0;
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                var a = expected.GetPixel(x, y);
                var b = actual.GetPixel(x, y);
                total += Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue);
            }
        }

        return total / (expected.Width * expected.Height * 3.0);
    }

    private static void AssertColor(SKColor expected, SKColor actual)
    {
        // JPEG compression shifts solid colors slightly.
        Assert.InRange(actual.Red, Math.Max(expected.Red - 12, 0), Math.Min(expected.Red + 12, 255));
        Assert.InRange(actual.Green, Math.Max(expected.Green - 12, 0), Math.Min(expected.Green + 12, 255));
        Assert.InRange(actual.Blue, Math.Max(expected.Blue - 12, 0), Math.Min(expected.Blue + 12, 255));
    }
}
