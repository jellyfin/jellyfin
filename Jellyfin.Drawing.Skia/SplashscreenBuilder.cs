using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Used to build the splashscreen.
    /// </summary>
    public class SplashscreenBuilder
    {
        private const int Rows = 6;
        private const int Spacing = 20;

        private readonly SkiaEncoder _skiaEncoder;

        private Random? _random;
        private int _finalWidth;
        private int _finalHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplashscreenBuilder"/> class.
        /// </summary>
        /// <param name="skiaEncoder">The SkiaEncoder.</param>
        public SplashscreenBuilder(SkiaEncoder skiaEncoder)
        {
            _skiaEncoder = skiaEncoder;
        }

        /// <summary>
        /// Generate a splashscreen.
        /// </summary>
        /// <param name="options">The options to generate the splashscreen.</param>
        public void GenerateSplash(SplashscreenOptions options)
        {
            _finalWidth = options.Width;
            _finalHeight = options.Height;
            var wall = GenerateCollage(options.PortraitInputPaths, options.LandscapeInputPaths, options.ApplyFilter);
            var transformed = Transform3D(wall);

            using var outputStream = new SKFileWStream(options.OutputPath);
            using var pixmap = new SKPixmap(new SKImageInfo(_finalWidth, _finalHeight), transformed.GetPixels());
            pixmap.Encode(outputStream, StripCollageBuilder.GetEncodedFormat(options.OutputPath), 90);
        }

        /// <summary>
        /// Generates a collage of posters and landscape pictures.
        /// </summary>
        /// <param name="poster">The poster paths.</param>
        /// <param name="backdrop">The landscape paths.</param>
        /// <param name="applyFilter">Whether to apply the darkening filter.</param>
        /// <returns>The created collage as a bitmap.</returns>
        private SKBitmap GenerateCollage(IReadOnlyList<string> poster, IReadOnlyList<string> backdrop, bool applyFilter)
        {
            _random = new Random();

            var posterIndex = 0;
            var backdropIndex = 0;

            // use higher resolution than final image
            var bitmap = new SKBitmap(_finalWidth * 3, _finalHeight * 2);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);

            int posterHeight = _finalHeight * 2 / 6;

            for (int i = 0; i < Rows; i++)
            {
                int imageCounter = _random.Next(0, 5);
                int currentWidthPos = i * 75;
                int currentHeight = i * (posterHeight + Spacing);

                while (currentWidthPos < _finalWidth * 3)
                {
                    SKBitmap? currentImage;

                    switch (imageCounter)
                    {
                        case 0:
                        case 2:
                        case 3:
                            currentImage = SkiaHelper.GetNextValidImage(_skiaEncoder, poster, posterIndex, out int newPosterIndex);
                            posterIndex = newPosterIndex;
                            break;
                        default:
                            currentImage = SkiaHelper.GetNextValidImage(_skiaEncoder, backdrop, backdropIndex, out int newBackdropIndex);
                            backdropIndex = newBackdropIndex;
                            break;
                    }

                    if (currentImage == null)
                    {
                        throw new ArgumentException("Not enough valid pictures provided to create a splashscreen!");
                    }

                    // resize to the same aspect as the original
                    var imageWidth = Math.Abs(posterHeight * currentImage.Width / currentImage.Height);
                    using var resizedBitmap = new SKBitmap(imageWidth, posterHeight);
                    currentImage.ScalePixels(resizedBitmap, SKFilterQuality.High);

                    // draw on canvas
                    canvas.DrawBitmap(resizedBitmap, currentWidthPos, currentHeight);

                    currentWidthPos += imageWidth + Spacing;

                    currentImage.Dispose();

                    if (imageCounter >= 4)
                    {
                        imageCounter = 0;
                    }
                    else
                    {
                        imageCounter++;
                    }
                }
            }

            if (applyFilter)
            {
                var paintColor = new SKPaint
                {
                    Color = SKColors.Black.WithAlpha(0x50),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(0, 0, _finalWidth * 3, _finalHeight * 2, paintColor);
            }

            return bitmap;
        }

        /// <summary>
        /// Transform the collage in 3D space.
        /// </summary>
        /// <param name="input">The bitmap to transform.</param>
        /// <returns>The transformed image.</returns>
        private SKBitmap Transform3D(SKBitmap input)
        {
            var bitmap = new SKBitmap(_finalWidth, _finalHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);
            var matrix = new SKMatrix
            {
                ScaleX = 0.324108899f,
                ScaleY = 0.563934922f,
                SkewX = -0.244337708f,
                SkewY = 0.0377609022f,
                TransX = 42.0407715f,
                TransY = -198.104706f,
                Persp0 = -9.08959337E-05f,
                Persp1 = 6.85242048E-05f,
                Persp2 = 0.988209724f
            };

            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(input, 0, 0);

            return bitmap;
        }
    }
}
