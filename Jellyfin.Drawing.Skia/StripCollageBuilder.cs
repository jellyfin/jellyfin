using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Used to build collages of multiple images arranged in vertical strips.
    /// </summary>
    public class StripCollageBuilder
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

        /// <summary>
        /// Check which format an image has been encoded with using its filename extension.
        /// </summary>
        /// <param name="outputPath">The path to the image to get the format for.</param>
        /// <returns>The image format.</returns>
        public static SKEncodedImageFormat GetEncodedFormat(string outputPath)
        {
            if (outputPath == null)
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            var ext = Path.GetExtension(outputPath);

            if (string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Jpeg;
            }

            if (string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Webp;
            }

            if (string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Gif;
            }

            if (string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase))
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
        /// <param name="itemName">The name of the library to draw on the collage.</param>
        public void BuildSquareCollage(IReadOnlyList<string> paths, string outputPath, int width, int height, string? itemName)
        {
            using var bitmap = BuildCollageBitmap(paths, width, height, itemName);
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
        /// <param name="itemName">The name of the library to draw on the collage.</param>
        public void BuildThumbCollage(IReadOnlyList<string> paths, string outputPath, int width, int height, string? itemName)
        {
            using var bitmap = BuildCollageBitmap(paths, width, height, itemName);
            using var outputStream = new SKFileWStream(outputPath);
            using var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels());
            pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
        }

        /// <summary>
        /// Create a poster collage.
        /// </summary>
        /// <param name="paths">The paths of the images to use in the collage.</param>
        /// <param name="outputPath">The path at which to place the resulting collage image.</param>
        /// <param name="width">The desired width of the collage.</param>
        /// <param name="height">The desired height of the collage.</param>
        /// <param name="itemName">The name of the library to draw on the collage.</param>
        public void BuildPosterCollage(IReadOnlyList<string> paths, string outputPath, int width, int height, string? itemName)
        {
            using var bitmap = BuildCollageBitmap(paths, width, height, itemName);
            using var outputStream = new SKFileWStream(outputPath);
            using var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels());
            pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
        }

        private SKBitmap BuildCollageBitmap(IReadOnlyList<string> paths, int width, int height, string? itemName)
        {
            var bitmap = new SKBitmap(width, height);

            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);

            using var backdrop = GetNextValidImage(paths, 0, out _);
            if (backdrop == null)
            {
                return bitmap;
            }

            // Resize the image to fill the canvas
            var ratioW = width / backdrop.Width;
            var ratioH = height / backdrop.Height;
            var ratio = ratioW < ratioH ? ratioW : ratioH;

            var newWidth = backdrop.Width * ratio;
            var newHeight = backdrop.Height * ratio;

            using var resizedBackdrop = SkiaEncoder.ResizeImage(backdrop, new SKImageInfo(newWidth, newHeight, backdrop.ColorType, backdrop.AlphaType, backdrop.ColorSpace));

            // Draw the original image in the center of the canvas
            canvas.DrawImage(resizedBackdrop, (width / 2) - (resizedBackdrop.Width / 2), (height / 2) - (resizedBackdrop.Height / 2));

            // draw shadow rectangle
            using var paintColor = new SKPaint
            {
                Color = new SKColor(53, 40, 77),
                Style = SKPaintStyle.Fill,
                BlendMode = SKBlendMode.Color
            };
            canvas.DrawRect(0, 0, width, height, paintColor);

            // Darken the image for text readability
            var darkenColor = new SKPaint
            {
                Color = SKColors.Black.WithAlpha(0x7D),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(0, 0, width, height, darkenColor);

            var typeFace = SKTypeface.FromFamilyName("sans-serif", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            // use the system fallback to find a typeface for the given CJK character
            var nonCjkPattern = @"[^\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}\p{IsKatakana}\p{IsHiragana}\p{IsHangulSyllables}\p{IsHangulJamo}]";
            var filteredName = Regex.Replace(itemName ?? string.Empty, nonCjkPattern, string.Empty);
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
            var textWidth = textPaint.MeasureText(itemName);
            if (textWidth > width * 0.95)
            {
                textPaint.TextSize = 0.9f * width * textPaint.TextSize / textWidth;
            }

            canvas.DrawText(itemName, width / 2f, (height / 2f) + (textPaint.FontMetrics.XHeight / 2), textPaint);

            return bitmap;
        }

        private SKBitmap? GetNextValidImage(IReadOnlyList<string> paths, int currentIndex, out int newIndex)
        {
            var imagesTested = new Dictionary<int, int>();
            SKBitmap? bitmap = null;

            while (imagesTested.Count < paths.Count)
            {
                if (currentIndex >= paths.Count)
                {
                    currentIndex = 0;
                }

                bitmap = _skiaEncoder.Decode(paths[currentIndex], false, null, out _);

                imagesTested[currentIndex] = 0;

                currentIndex++;

                if (bitmap != null)
                {
                    break;
                }
            }

            newIndex = currentIndex;
            return bitmap;
        }
    }
}
