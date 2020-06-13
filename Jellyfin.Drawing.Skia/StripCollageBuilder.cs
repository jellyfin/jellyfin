using System;
using System.Collections.Generic;
using System.IO;
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
        public void BuildSquareCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildSquareCollageBitmap(paths, width, height))
            using (var outputStream = new SKFileWStream(outputPath))
            using (var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels()))
            {
                pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
            }
        }

        /// <summary>
        /// Create a thumb collage.
        /// </summary>
        /// <param name="paths">The paths of the images to use in the collage.</param>
        /// <param name="outputPath">The path at which to place the resulting image.</param>
        /// <param name="width">The desired width of the collage.</param>
        /// <param name="height">The desired height of the collage.</param>
        public void BuildThumbCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildThumbCollageBitmap(paths, width, height))
            using (var outputStream = new SKFileWStream(outputPath))
            using (var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels()))
            {
                pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
            }
        }

        private SKBitmap BuildThumbCollageBitmap(string[] paths, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Black);

                // number of images used in the thumbnail
                var iCount = 3;

                // determine sizes for each image that will composited into the final image
                var iSlice = Convert.ToInt32(width / iCount);
                int iHeight = Convert.ToInt32(height * 1.00);
                int imageIndex = 0;
                for (int i = 0; i < iCount; i++)
                {
                    using (var currentBitmap = GetNextValidImage(paths, imageIndex, out int newIndex))
                    {
                        imageIndex = newIndex;
                        if (currentBitmap == null)
                        {
                            continue;
                        }

                        // resize to the same aspect as the original
                        int iWidth = Math.Abs(iHeight * currentBitmap.Width / currentBitmap.Height);
                        using (var resizeBitmap = new SKBitmap(iWidth, iHeight, currentBitmap.ColorType, currentBitmap.AlphaType))
                        {
                            currentBitmap.ScalePixels(resizeBitmap, SKFilterQuality.High);

                            // crop image
                            int ix = Math.Abs((iWidth - iSlice) / 2);
                            using (var image = SKImage.FromBitmap(resizeBitmap))
                            using (var subset = image.Subset(SKRectI.Create(ix, 0, iSlice, iHeight)))
                            {
                                // draw image onto canvas
                                canvas.DrawImage(subset ?? image, iSlice * i, 0);
                            }
                        }
                    }
                }
            }

            return bitmap;
        }

        private SKBitmap? GetNextValidImage(string[] paths, int currentIndex, out int newIndex)
        {
            var imagesTested = new Dictionary<int, int>();
            SKBitmap? bitmap = null;

            while (imagesTested.Count < paths.Length)
            {
                if (currentIndex >= paths.Length)
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

        private SKBitmap BuildSquareCollageBitmap(string[] paths, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);
            var imageIndex = 0;
            var cellWidth = width / 2;
            var cellHeight = height / 2;

            using (var canvas = new SKCanvas(bitmap))
            {
                for (var x = 0; x < 2; x++)
                {
                    for (var y = 0; y < 2; y++)
                    {
                        using (var currentBitmap = GetNextValidImage(paths, imageIndex, out int newIndex))
                        {
                            imageIndex = newIndex;

                            if (currentBitmap == null)
                            {
                                continue;
                            }

                            using (var resizedBitmap = new SKBitmap(cellWidth, cellHeight, currentBitmap.ColorType, currentBitmap.AlphaType))
                            {
                                // scale image
                                currentBitmap.ScalePixels(resizedBitmap, SKFilterQuality.High);

                                // draw this image into the strip at the next position
                                var xPos = x * cellWidth;
                                var yPos = y * cellHeight;
                                canvas.DrawBitmap(resizedBitmap, xPos, yPos);
                            }
                        }
                    }
                }
            }

            return bitmap;
        }
    }
}
