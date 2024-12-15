using System;
using System.Collections.Generic;
using System.Globalization;
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

        var typeFace = SkiaEncoder.DefaultTypeFace;

        // draw library name
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            TextSize = 112,
            TextAlign = SKTextAlign.Left,
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

        var realWidth = DrawText(null, 0, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), libraryName, textPaint);
        if (realWidth > width * 0.95)
        {
            textPaint.TextSize = 0.9f * width * textPaint.TextSize / realWidth;
            realWidth = DrawText(null, 0, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), libraryName, textPaint);
        }

        var padding = (width - realWidth) / 2;

        if (IsRtlTextRegex().IsMatch(libraryName))
        {
            textPaint.TextAlign = SKTextAlign.Right;
            DrawText(canvas, width - padding, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), libraryName, textPaint, true);
        }
        else
        {
            DrawText(canvas, padding, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), libraryName, textPaint);
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

    /// <summary>
    /// Draw shaped text with given SKPaint.
    /// </summary>
    /// <param name="canvas">If not null, draw text to this canvas, otherwise only measure the text width.</param>
    /// <param name="x">x position of the canvas to draw text.</param>
    /// <param name="y">y position of the canvas to draw text.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="textPaint">The SKPaint to style the text.</param>
    /// <returns>The width of the text.</returns>
    private static float DrawTextWithoutFallback(SKCanvas? canvas, float x, float y, string text, SKPaint textPaint)
    {
        var width = textPaint.MeasureText(text);
        canvas?.DrawShapedText(text, x, y, textPaint);
        return width;
    }

    /// <summary>
    /// Draw shaped text with given SKPaint, search defined type faces to render as many texts as possible.
    /// </summary>
    /// <param name="canvas">If not null, draw text to this canvas, otherwise only measure the text width.</param>
    /// <param name="x">x position of the canvas to draw text.</param>
    /// <param name="y">y position of the canvas to draw text.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="textPaint">The SKPaint to style the text.</param>
    /// <param name="isRtl">If true, render from right to left.</param>
    /// <returns>The width of the text.</returns>
    private static float DrawText(SKCanvas? canvas, float x, float y, string text, SKPaint textPaint, bool isRtl = false)
    {
        float width = 0;

        if (textPaint.ContainsGlyphs(text))
        {
            // Current font can render all characters in text
            return DrawTextWithoutFallback(canvas, x, y, text, textPaint);
        }

        // Iterate over all text elements using TextElementEnumerator
        // We cannot use foreach here because a human-readable character (grapheme cluster) can be multiple code points
        // We cannot render character by character because glyphs do not always have same width
        // And the result will look very unnatural due to the width difference and missing natural spacing
        var start = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            bool notAtEnd;
            var textElement = enumerator.GetTextElement();
            if (textPaint.ContainsGlyphs(textElement))
            {
                continue;
            }

            // If we get here, we have a text element which cannot be rendered with current font
            // Draw previous characters which can be rendered with current font
            if (start != enumerator.ElementIndex)
            {
                var regularText = text.Substring(start, enumerator.ElementIndex - start);
                width += DrawTextWithoutFallback(canvas, MoveX(x, width), y, regularText, textPaint);
                start = enumerator.ElementIndex;
            }

            // Search for next point where current font can render the character there
            while ((notAtEnd = enumerator.MoveNext()) && !textPaint.ContainsGlyphs(enumerator.GetTextElement()))
            {
                // Do nothing, just move enumerator to the point where current font can render the character
            }

            // Now we have a substring that should pick another font
            // The enumerator may or may not be already at the end of the string
            var subtext = notAtEnd
                ? text.Substring(start, enumerator.ElementIndex - start)
                : text[start..];

            var fallback = SkiaEncoder.GetFontForCharacter(textElement);

            if (fallback is not null)
            {
                using var fallbackTextPaint = new SKPaint();
                fallbackTextPaint.Color = textPaint.Color;
                fallbackTextPaint.Style = textPaint.Style;
                fallbackTextPaint.TextSize = textPaint.TextSize;
                fallbackTextPaint.TextAlign = textPaint.TextAlign;
                fallbackTextPaint.Typeface = fallback;
                fallbackTextPaint.IsAntialias = textPaint.IsAntialias;

                // Do the search recursively to select all possible fonts
                width += DrawText(canvas, MoveX(x, width), y, subtext, fallbackTextPaint, isRtl);
            }
            else
            {
                // Used up all fonts and no fonts can be found, just use current font
                width += DrawTextWithoutFallback(canvas, MoveX(x, width), y, text[start..], textPaint);
            }

            start = notAtEnd ? enumerator.ElementIndex : text.Length;
        }

        // Render the remaining text that current fonts can render
        if (start < text.Length)
        {
            width += DrawTextWithoutFallback(canvas, MoveX(x, width), y, text[start..], textPaint);
        }

        return width;
        float MoveX(float currentX, float dWidth) => isRtl ? currentX - dWidth : currentX + dWidth;
    }
}
