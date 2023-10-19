using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Used to build collages of multiple images arranged in vertical strips.
/// </summary>
public partial class StripCollageBuilder
{
    private readonly SkiaEncoder _skiaEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripCollageBuilder"/> class.
    /// </summary>
    /// <param name="skiaEncoder">The encoder to use for building collages.</param>
    public StripCollageBuilder(SkiaEncoder skiaEncoder)
    {
        _skiaEncoder = skiaEncoder;
    }

    [GeneratedRegex(@"[^\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}\p{IsKatakana}\p{IsHiragana}\p{IsHangulSyllables}\p{IsHangulJamo}]")]
    private static partial Regex NonCjkPatternRegex();

    [GeneratedRegex(@"\p{IsArabic}|\p{IsArmenian}|\p{IsHebrew}|\p{IsSyriac}|\p{IsThaana}")]
    private static partial Regex IsRtlTextRegex();

    /// <summary>
    /// Check which format an image has been encoded with using its filename extension.
    /// </summary>
    /// <param name="outputPath">The path to the image to get the format for.</param>
    /// <returns>The image format.</returns>
    public static SKEncodedImageFormat GetEncodedFormat(string outputPath)
    {
        ArgumentNullException.ThrowIfNull(outputPath);

        var ext = Path.GetExtension(outputPath.AsSpan());

        if (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Jpeg;
        }

        if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Webp;
        }

        if (ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Gif;
        }

        if (ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Bmp;
        }

        // default to png
        return SKEncodedImageFormat.Png;
    }

    /// <summary>
    /// Create a square collage.
    /// </summary>
    /// <param name="paths">The paths of the images to use in the collage.</param>
    /// <param name="outputPath">The path at which to place the resulting collage image.</param>
    /// <param name="width">The desired width of the collage.</param>
    /// <param name="height">The desired height of the collage.</param>
    public void BuildSquareCollage(IReadOnlyList<string> paths, string outputPath, int width, int height)
    {
        using var bitmap = BuildSquareCollageBitmap(paths, width, height);
        using var outputStream = new SKFileWStream(outputPath);
        using var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels());
        pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
    }

    /// <summary>
    /// Create a thumb collage.
    /// </summary>
    /// <param name="paths">The paths of the images to use in the collage.</param>
    /// <param name="outputPath">The path at which to place the resulting image.</param>
    /// <param name="width">The desired width of the collage.</param>
    /// <param name="height">The desired height of the collage.</param>
    /// <param name="libraryName">The name of the library to draw on the collage.</param>
    public void BuildThumbCollage(IReadOnlyList<string> paths, string outputPath, int width, int height, string? libraryName)
    {
        using var bitmap = BuildThumbCollageBitmap(paths, width, height, libraryName);
        using var outputStream = new SKFileWStream(outputPath);
        using var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels());
        pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
    }

    private SKBitmap BuildThumbCollageBitmap(IReadOnlyList<string> paths, int width, int height, string? libraryName)
    {
        var bitmap = new SKBitmap(width, height);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        using var backdrop = SkiaHelper.GetNextValidImage(_skiaEncoder, paths, 0, out _);
        if (backdrop is null)
        {
            return bitmap;
        }

        // resize to the same aspect as the original
        var backdropHeight = Math.Abs(width * backdrop.Height / backdrop.Width);
        using var residedBackdrop = SkiaEncoder.ResizeImage(backdrop, new SKImageInfo(width, backdropHeight, backdrop.ColorType, backdrop.AlphaType, backdrop.ColorSpace));
        // draw the backdrop
        canvas.DrawImage(residedBackdrop, 0, 0);

        // draw shadow rectangle
        using var paintColor = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(0x78),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, width, height, paintColor);

        var typeFace = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        // use the system fallback to find a typeface for the given CJK character
        var filteredName = NonCjkPatternRegex().Replace(libraryName ?? string.Empty, string.Empty);
        if (!string.IsNullOrEmpty(filteredName))
        {
            typeFace = SKFontManager.Default.MatchCharacter(null, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, null, filteredName[0]);
        }

        // draw library name
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            TextSize = 112,
            TextAlign = SKTextAlign.Center,
            Typeface = typeFace,
            IsAntialias = true
        };

        // scale down text to 90% of the width if text is larger than 95% of the width
        var textWidth = textPaint.MeasureText(libraryName);
        if (textWidth > width * 0.95)
        {
            textPaint.TextSize = 0.9f * width * textPaint.TextSize / textWidth;
        }

        if (string.IsNullOrWhiteSpace(libraryName))
        {
            return bitmap;
        }

        if (IsRtlTextRegex().IsMatch(libraryName))
        {
            canvas.DrawShapedText(libraryName, width / 2f, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), textPaint);
        }
        else
        {
            canvas.DrawText(libraryName, width / 2f, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), textPaint);
        }

        return bitmap;
    }

    private SKBitmap BuildSquareCollageBitmap(IReadOnlyList<string> paths, int width, int height)
    {
        var bitmap = new SKBitmap(width, height);
        var imageIndex = 0;
        var cellWidth = width / 2;
        var cellHeight = height / 2;

        using var canvas = new SKCanvas(bitmap);
        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                using var currentBitmap = SkiaHelper.GetNextValidImage(_skiaEncoder, paths, imageIndex, out int newIndex);
                imageIndex = newIndex;

                if (currentBitmap is null)
                {
                    continue;
                }

                // Scale image. The FromBitmap creates a copy
                var imageInfo = new SKImageInfo(cellWidth, cellHeight, currentBitmap.ColorType, currentBitmap.AlphaType, currentBitmap.ColorSpace);
                using var resizeImage = SkiaEncoder.ResizeImage(currentBitmap, imageInfo);

                // draw this image into the strip at the next position
                var xPos = x * cellWidth;
                var yPos = y * cellHeight;
                canvas.DrawImage(resizeImage, xPos, yPos);
            }
        }

        return bitmap;
    }
}
