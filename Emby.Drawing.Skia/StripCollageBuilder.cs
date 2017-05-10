using SkiaSharp;
using MediaBrowser.Common.Configuration;
using System;
using System.IO;
using MediaBrowser.Model.IO;

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
                    bitmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
                }
            }
        }

        public void BuildThumbCollage(string[] paths, string outputPath, int width, int height)
        {
            using (var bitmap = BuildThumbCollageBitmap(paths, width, height))
            {
                using (var outputStream = new SKFileWStream(outputPath))
                {
                    bitmap.Encode(outputStream, GetEncodedFormat(outputPath), 90);
                }
            }
        }

        private SKBitmap BuildThumbCollageBitmap(string[] paths, int width, int height)
        {
            var bitmap = new SKBitmap(width, height);

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.Black);

                var iSlice = Convert.ToInt32(width * 0.24125);
                int iTrans = Convert.ToInt32(height * .25);
                int iHeight = Convert.ToInt32(height * .70);
                var horizontalImagePadding = Convert.ToInt32(width * 0.0125);
                var verticalSpacing = Convert.ToInt32(height * 0.01111111111111111111111111111111);
                int imageIndex = 0;

                for (int i = 0; i < 4; i++)
                {
                    using (var currentBitmap = SKBitmap.Decode(paths[imageIndex]))
                    {
                        int iWidth = (int)Math.Abs(iHeight * currentBitmap.Width / currentBitmap.Height);
                        using (var resizeBitmap = new SKBitmap(iWidth, iHeight, currentBitmap.ColorType, currentBitmap.AlphaType))
                        {
                            currentBitmap.Resize(resizeBitmap, SKBitmapResizeMethod.Lanczos3);
                            int ix = (int)Math.Abs((iWidth - iSlice) / 2);
                            using (var image = SKImage.FromBitmap(resizeBitmap))
                            {
                                using (var subset = image.Subset(SKRectI.Create(ix, 0, iSlice, iHeight)))
                                {
                                    canvas.DrawImage(subset, (horizontalImagePadding * (i + 1)) + (iSlice * i), 0);

                                    using (var croppedBitmap = SKBitmap.FromImage(subset))
                                    {
                                        using (var flipped = new SKBitmap(croppedBitmap.Width, croppedBitmap.Height / 2, croppedBitmap.ColorType, croppedBitmap.AlphaType))
                                        {
                                            croppedBitmap.Resize(flipped, SKBitmapResizeMethod.Lanczos3);

                                            using (var gradient = new SKPaint())
                                            {
                                                var matrix = SKMatrix.MakeScale(1, -1);
                                                matrix.SetScaleTranslate(1, -1, 0, flipped.Height);
                                                gradient.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, flipped.Height), new[] { new SKColor(0, 0, 0, 0), SKColors.Black }, null, SKShaderTileMode.Clamp, matrix);
                                                canvas.DrawBitmap(flipped, (horizontalImagePadding * (i + 1)) + (iSlice * i), iHeight + verticalSpacing, gradient);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    imageIndex++;

                    if (imageIndex >= paths.Length)
                        imageIndex = 0;
                }
            }

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
                        using (var currentBitmap = SKBitmap.Decode(paths[imageIndex]))
                        {
                            using (var resizedBitmap = new SKBitmap(cellWidth, cellHeight, currentBitmap.ColorType, currentBitmap.AlphaType))
                            {
                                // scale image
                                currentBitmap.Resize(resizedBitmap, SKBitmapResizeMethod.Lanczos3);

                                // draw this image into the strip at the next position
                                var xPos = x * cellWidth;
                                var yPos = y * cellHeight;
                                canvas.DrawBitmap(resizedBitmap, xPos, yPos);
                            }
                        }
                        imageIndex++;

                        if (imageIndex >= paths.Length)
                            imageIndex = 0;
                    }
                }
            }

            return bitmap;
        }
    }
}