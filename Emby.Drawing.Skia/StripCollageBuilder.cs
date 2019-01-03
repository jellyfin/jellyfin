using SkiaSharp;
using MediaBrowser.Common.Configuration;
using System;
using System.IO;
using MediaBrowser.Model.IO;
using System.Collections.Generic;

namespace Emby.Drawing.Skia
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
            var ext = Path.GetExtension(outputPath).ToLower();

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

        public void BuildPosterCollage(string[] paths, string outputPath, int width, int height)
        {
            // @todo
        }

        public void BuildSquareCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildSquareCollageBitmap(paths, width, height))
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
                var iSlice = Convert.ToInt32(width * 0.23475);
                int iTrans = Convert.ToInt32(height * .25);
                int iHeight = Convert.ToInt32(height * .70);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0125);
                var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                int imageIndex = 0;

                for (int i = 0; i < 4; i++)
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
                                canvas.DrawImage(subset ?? image, (horizontalImagePadding * (i + 1)) + (iSlice * i), verticalSpacing);

                                if (subset == null)
                                {
                                    continue;
                                }
                                // create reflection of image below the drawn image
                                using (var croppedBitmap = SKBitmap.FromImage(subset))
                                using (var reflectionBitmap = new SKBitmap(croppedBitmap.Width, croppedBitmap.Height / 2, croppedBitmap.ColorType, croppedBitmap.AlphaType))
                                {
                                    // resize to half height
                                    currentBitmap.ScalePixels(reflectionBitmap, SKFilterQuality.High);

                                    using (var flippedBitmap = new SKBitmap(reflectionBitmap.Width, reflectionBitmap.Height, reflectionBitmap.ColorType, reflectionBitmap.AlphaType))
                                    using (var flippedCanvas = new SKCanvas(flippedBitmap))
                                    {
                                        // flip image vertically
                                        var matrix = SKMatrix.MakeScale(1, -1);
                                        matrix.SetScaleTranslate(1, -1, 0, flippedBitmap.Height);
                                        flippedCanvas.SetMatrix(matrix);
                                        flippedCanvas.DrawBitmap(reflectionBitmap, 0, 0);
                                        flippedCanvas.ResetMatrix();

                                        // create gradient to make image appear as a reflection
                                        var remainingHeight = height - (iHeight + (2 * verticalSpacing));
                                        flippedCanvas.ClipRect(SKRect.Create(reflectionBitmap.Width, remainingHeight));
                                        using (var gradient = new SKPaint())
                                        {
                                            gradient.IsAntialias = true;
                                            gradient.BlendMode = SKBlendMode.SrcOver;
                                            gradient.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, remainingHeight), new[] { new SKColor(0, 0, 0, 128), new SKColor(0, 0, 0, 208), new SKColor(0, 0, 0, 240), new SKColor(0, 0, 0, 255) }, null, SKShaderTileMode.Clamp);
                                            flippedCanvas.DrawPaint(gradient);
                                        }

                                        // finally draw reflection onto canvas
                                        canvas.DrawBitmap(flippedBitmap, (horizontalImagePadding * (i + 1)) + (iSlice * i), iHeight + (2 * verticalSpacing));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return bitmap;
        }

        private SKBitmap GetNextValidImage(string[] paths, int currentIndex, out int newIndex)
        {
            Dictionary<int, int> imagesTested = new Dictionary<int, int>();
            SKBitmap bitmap = null;

            while (imagesTested.Count < paths.Length)
            {
                if (currentIndex >= paths.Length)
                {
                    currentIndex = 0;
                }

                bitmap = SkiaEncoder.Decode(paths[currentIndex], false, _fileSystem, null, out SKEncodedOrigin origin);

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
