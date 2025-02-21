using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Used to build the splashscreen.
/// </summary>
public class SplashscreenBuilder
{
    private const int FinalWidth = 1920;
    private const int FinalHeight = 1080;
    // generated collage resolution should be greater than the final resolution
    private const int WallWidth = FinalWidth * 3;
    private const int WallHeight = FinalHeight * 2;
    private const int Rows = 6;
    private const int Spacing = 20;

    private readonly SkiaEncoder _skiaEncoder;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SplashscreenBuilder"/> class.
    /// </summary>
    /// <param name="skiaEncoder">The SkiaEncoder.</param>
    /// <param name="logger">The logger.</param>
    public SplashscreenBuilder(SkiaEncoder skiaEncoder, ILogger logger)
    {
        _skiaEncoder = skiaEncoder;
        _logger = logger;
    }

    /// <summary>
    /// Generate a splashscreen.
    /// </summary>
    /// <param name="posters">The poster paths.</param>
    /// <param name="backdrops">The landscape paths.</param>
    /// <param name="outputPath">The output path.</param>
    public void GenerateSplash(IReadOnlyList<string> posters, IReadOnlyList<string> backdrops, string outputPath)
    {
        using var wall = GenerateCollage(posters, backdrops);
        using var transformed = Transform3D(wall);

        using var outputStream = new SKFileWStream(outputPath);
        using var pixmap = new SKPixmap(new SKImageInfo(FinalWidth, FinalHeight), transformed.GetPixels());
        pixmap.Encode(outputStream, StripCollageBuilder.GetEncodedFormat(outputPath), 90);
    }

    /// <summary>
    /// Generates a collage of posters and landscape pictures.
    /// </summary>
    /// <param name="posters">The poster paths.</param>
    /// <param name="backdrops">The landscape paths.</param>
    /// <returns>The created collage as a bitmap.</returns>
    private SKBitmap GenerateCollage(IReadOnlyList<string> posters, IReadOnlyList<string> backdrops)
    {
        var posterIndex = 0;
        var backdropIndex = 0;

        SKBitmap? bitmap = null;
        try
        {
            bitmap = new SKBitmap(WallWidth, WallHeight);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);

            int posterHeight = WallHeight / 6;

            for (int i = 0; i < Rows; i++)
            {
                int imageCounter = Random.Shared.Next(0, 5);
                int currentWidthPos = i * 75;
                int currentHeight = i * (posterHeight + Spacing);

                while (currentWidthPos < WallWidth)
                {
                    SKBitmap? currentImage;

                    switch (imageCounter)
                    {
                        case 0:
                        case 2:
                        case 3:
                            currentImage = SkiaHelper.GetNextValidImage(_skiaEncoder, posters, posterIndex, out int newPosterIndex);
                            posterIndex = newPosterIndex;
                            break;
                        default:
                            currentImage = SkiaHelper.GetNextValidImage(_skiaEncoder, backdrops, backdropIndex, out int newBackdropIndex);
                            backdropIndex = newBackdropIndex;
                            break;
                    }

                    if (currentImage is null)
                    {
                        throw new ArgumentException("Not enough valid pictures provided to create a splashscreen!");
                    }

                    using (currentImage)
                    {
                        var imageWidth = Math.Abs(posterHeight * currentImage.Width / currentImage.Height);
                        using var resizedBitmap = new SKBitmap(imageWidth, posterHeight);
                        currentImage.ScalePixels(resizedBitmap, SKFilterQuality.High);

                        // draw on canvas
                        canvas.DrawBitmap(resizedBitmap, currentWidthPos, currentHeight);

                        // resize to the same aspect as the original
                        currentWidthPos += imageWidth + Spacing;
                    }

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

            return bitmap;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Detected intermediary error creating splashscreen image");
            bitmap?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Transform the collage in 3D space.
    /// </summary>
    /// <param name="input">The bitmap to transform.</param>
    /// <returns>The transformed image.</returns>
    private SKBitmap Transform3D(SKBitmap input)
    {
        SKBitmap? bitmap = null;
        try
        {
            bitmap = new SKBitmap(FinalWidth, FinalHeight);
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
        catch (Exception e)
        {
            _logger.LogError(e, "Detected intermediary error creating splashscreen image transforming the image");
            bitmap?.Dispose();
            throw;
        }
    }
}
