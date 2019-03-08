using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    public class StripCollageBuilder
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public StripCollageBuilder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public static SKEncodedImageFormat GetEncodedFormat(string outputPath)
        {
            if (outputPath == null)
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            var ext = Path.GetExtension(outputPath).ToLowerInvariant();

            if (ext == ".jpg" || ext == ".jpeg")
                return SKEncodedImageFormat.Jpeg;

            if (ext == ".webp")
                return SKEncodedImageFormat.Webp;

            if (ext == ".gif")
                return SKEncodedImageFormat.Gif;

            if (ext == ".bmp")
                return SKEncodedImageFormat.Bmp;

            // default to png
            return SKEncodedImageFormat.Png;
        }

        public void BuildSquareCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildSquareCollageBitmap(paths, width, height))
            using (var outputStream = new SKFileWStream(outputPath))
            {
                using (var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels()))
                {
                    pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
                }
            }
        }

        public void BuildThumbCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildThumbCollageBitmap(paths, width, height))
            {
                using (var outputStream = new SKFileWStream(outputPath))
                {
                    using (var pixmap = new SKPixmap(new SKImageInfo(width, height), bitmap.GetPixels()))
                    {
                        pixmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
                    }
                }
            }
        }

        private SKBitmap BuildThumbCollageBitmap(string[] paths, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Black);

                // determine sizes for each image that will composited into the final image
                var iSlice = Convert.ToInt32(width * 0.33);
                int iTrans = Convert.ToInt32(height * 0.25);
                int iHeight = Convert.ToInt32(height * 1.00);
                int imageIndex = 0;
                for (int i = 0; i < 3; i++)
                {
                    using (var currentBitmap = GetNextValidImage(paths, imageIndex, out int newIndex))
                    {
                        imageIndex = newIndex;
                        if (currentBitmap == null)
                        {
                            continue;
                        }

                        // resize to the same aspect as the original
                        int iWidth = (int)Math.Abs(iHeight * currentBitmap.Width / currentBitmap.Height);
                        using (var resizeBitmap = new SKBitmap(iWidth, iHeight, currentBitmap.ColorType, currentBitmap.AlphaType))
                        {
                            currentBitmap.ScalePixels(resizeBitmap, SKFilterQuality.High);
                            // crop image
                            int ix = (int)Math.Abs((iWidth - iSlice) / 2);
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

        private SKBitmap GetNextValidImage(string[] paths, int currentIndex, out int newIndex)
        {
            var imagesTested = new Dictionary<int, int>();
            SKBitmap bitmap = null;

            while (imagesTested.Count < paths.Length)
            {
                if (currentIndex >= paths.Length)
                {
                    currentIndex = 0;
                }

                bitmap = SkiaEncoder.Decode(paths[currentIndex], false, _fileSystem, null, out var origin);

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
