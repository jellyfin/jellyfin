using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BlurHashSharp.SkiaSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Drawing;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using static Jellyfin.Drawing.Skia.SkiaHelper;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Image encoder that uses <see cref="SkiaSharp"/> to manipulate images.
    /// </summary>
    public class SkiaEncoder : IImageEncoder
    {
        private static readonly HashSet<string> _transparentImageTypes
            = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".gif", ".webp" };

        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkiaEncoder"/> class.
        /// </summary>
        /// <param name="logger">The application logger.</param>
        /// <param name="appPaths">The application paths.</param>
        public SkiaEncoder(
            ILogger<SkiaEncoder> logger,
            IApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;
        }

        /// <inheritdoc/>
        public string Name => "Skia";

        /// <inheritdoc/>
        public bool SupportsImageCollageCreation => true;

        /// <inheritdoc/>
        public bool SupportsImageEncoding => true;

        /// <inheritdoc/>
        public IReadOnlyCollection<string> SupportedInputFormats =>
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jpeg",
                "jpg",
                "png",
                "dng",
                "webp",
                "gif",
                "bmp",
                "ico",
                "astc",
                "ktx",
                "pkm",
                "wbmp",
                // TODO: check if these are supported on multiple platforms
                // https://github.com/google/skia/blob/master/infra/bots/recipes/test.py#L454
                // working on windows at least
                "cr2",
                "nef",
                "arw"
            };

        /// <inheritdoc/>
        public IReadOnlyCollection<ImageFormat> SupportedOutputFormats
            => new HashSet<ImageFormat>() { ImageFormat.Webp, ImageFormat.Jpg, ImageFormat.Png };

        /// <summary>
        /// Check if the native lib is available.
        /// </summary>
        /// <returns>True if the native lib is available, otherwise false.</returns>
        public static bool IsNativeLibAvailable()
        {
            try
            {
                // test an operation that requires the native library
                SKPMColor.PreMultiply(SKColors.Black);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsTransparent(SKColor color)
            => (color.Red == 255 && color.Green == 255 && color.Blue == 255) || color.Alpha == 0;

        /// <summary>
        /// Convert a <see cref="ImageFormat"/> to a <see cref="SKEncodedImageFormat"/>.
        /// </summary>
        /// <param name="selectedFormat">The format to convert.</param>
        /// <returns>The converted format.</returns>
        public static SKEncodedImageFormat GetImageFormat(ImageFormat selectedFormat)
        {
            switch (selectedFormat)
            {
                case ImageFormat.Bmp:
                    return SKEncodedImageFormat.Bmp;
                case ImageFormat.Jpg:
                    return SKEncodedImageFormat.Jpeg;
                case ImageFormat.Gif:
                    return SKEncodedImageFormat.Gif;
                case ImageFormat.Webp:
                    return SKEncodedImageFormat.Webp;
                default:
                    return SKEncodedImageFormat.Png;
            }
        }

        private static bool IsTransparentRow(SKBitmap bmp, int row)
        {
            for (var i = 0; i < bmp.Width; ++i)
            {
                if (!IsTransparent(bmp.GetPixel(i, row)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTransparentColumn(SKBitmap bmp, int col)
        {
            for (var i = 0; i < bmp.Height; ++i)
            {
                if (!IsTransparent(bmp.GetPixel(col, i)))
                {
                    return false;
                }
            }

            return true;
        }

        private SKBitmap CropWhiteSpace(SKBitmap bitmap)
        {
            var topmost = 0;
            for (int row = 0; row < bitmap.Height; ++row)
            {
                if (IsTransparentRow(bitmap, row))
                {
                    topmost = row + 1;
                }
                else
                {
                    break;
                }
            }

            int bottommost = bitmap.Height;
            for (int row = bitmap.Height - 1; row >= 0; --row)
            {
                if (IsTransparentRow(bitmap, row))
                {
                    bottommost = row;
                }
                else
                {
                    break;
                }
            }

            int leftmost = 0, rightmost = bitmap.Width;
            for (int col = 0; col < bitmap.Width; ++col)
            {
                if (IsTransparentColumn(bitmap, col))
                {
                    leftmost = col + 1;
                }
                else
                {
                    break;
                }
            }

            for (int col = bitmap.Width - 1; col >= 0; --col)
            {
                if (IsTransparentColumn(bitmap, col))
                {
                    rightmost = col;
                }
                else
                {
                    break;
                }
            }

            var newRect = SKRectI.Create(leftmost, topmost, rightmost - leftmost, bottommost - topmost);

            using (var image = SKImage.FromBitmap(bitmap))
            using (var subset = image.Subset(newRect))
            {
                return SKBitmap.FromImage(subset);
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="FileNotFoundException">The path is not valid.</exception>
        /// <exception cref="SkiaCodecException">The file at the specified path could not be used to generate a codec.</exception>
        public ImageDimensions GetImageSize(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found", path);
            }

            using (var codec = SKCodec.Create(path, out SKCodecResult result))
            {
                EnsureSuccess(result);

                var info = codec.Info;

                return new ImageDimensions(info.Width, info.Height);
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">The path is null.</exception>
        /// <exception cref="FileNotFoundException">The path is not valid.</exception>
        /// <exception cref="SkiaCodecException">The file at the specified path could not be used to generate a codec.</exception>
        public string GetImageBlurHash(int xComp, int yComp, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return BlurHashEncoder.Encode(xComp, yComp, path);
        }

        private static bool HasDiacritics(string text)
            => !string.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);

        private bool RequiresSpecialCharacterHack(string path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                if (char.GetUnicodeCategory(path[i]) == UnicodeCategory.OtherLetter)
                {
                    return true;
                }
            }

            if (HasDiacritics(path))
            {
                return true;
            }

            return false;
        }

        private string NormalizePath(string path)
        {
            if (!RequiresSpecialCharacterHack(path))
            {
                return path;
            }

            var tempPath = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + Path.GetExtension(path));

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
            File.Copy(path, tempPath, true);

            return tempPath;
        }

        private static SKEncodedOrigin GetSKEncodedOrigin(ImageOrientation? orientation)
        {
            if (!orientation.HasValue)
            {
                return SKEncodedOrigin.TopLeft;
            }

            switch (orientation.Value)
            {
                case ImageOrientation.TopRight:
                    return SKEncodedOrigin.TopRight;
                case ImageOrientation.RightTop:
                    return SKEncodedOrigin.RightTop;
                case ImageOrientation.RightBottom:
                    return SKEncodedOrigin.RightBottom;
                case ImageOrientation.LeftTop:
                    return SKEncodedOrigin.LeftTop;
                case ImageOrientation.LeftBottom:
                    return SKEncodedOrigin.LeftBottom;
                case ImageOrientation.BottomRight:
                    return SKEncodedOrigin.BottomRight;
                case ImageOrientation.BottomLeft:
                    return SKEncodedOrigin.BottomLeft;
                default:
                    return SKEncodedOrigin.TopLeft;
            }
        }

        /// <summary>
        /// Decode an image.
        /// </summary>
        /// <param name="path">The filepath of the image to decode.</param>
        /// <param name="forceCleanBitmap">Whether to force clean the bitmap.</param>
        /// <param name="orientation">The orientation of the image.</param>
        /// <param name="origin">The detected origin of the image.</param>
        /// <returns>The resulting bitmap of the image.</returns>
        internal SKBitmap? Decode(string path, bool forceCleanBitmap, ImageOrientation? orientation, out SKEncodedOrigin origin)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("File not found", path);
            }

            var requiresTransparencyHack = _transparentImageTypes.Contains(Path.GetExtension(path));

            if (requiresTransparencyHack || forceCleanBitmap)
            {
                using (var codec = SKCodec.Create(NormalizePath(path)))
                {
                    if (codec == null)
                    {
                        origin = GetSKEncodedOrigin(orientation);
                        return null;
                    }

                    // create the bitmap
                    var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, !requiresTransparencyHack);

                    // decode
                    _ = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                    origin = codec.EncodedOrigin;

                    return bitmap;
                }
            }

            var resultBitmap = SKBitmap.Decode(NormalizePath(path));

            if (resultBitmap == null)
            {
                return Decode(path, true, orientation, out origin);
            }

            // If we have to resize these they often end up distorted
            if (resultBitmap.ColorType == SKColorType.Gray8)
            {
                using (resultBitmap)
                {
                    return Decode(path, true, orientation, out origin);
                }
            }

            origin = SKEncodedOrigin.TopLeft;
            return resultBitmap;
        }

        private SKBitmap? GetBitmap(string path, bool cropWhitespace, bool forceAnalyzeBitmap, ImageOrientation? orientation, out SKEncodedOrigin origin)
        {
            if (cropWhitespace)
            {
                using (var bitmap = Decode(path, forceAnalyzeBitmap, orientation, out origin))
                {
                    if (bitmap == null)
                    {
                        return null;
                    }

                    return CropWhiteSpace(bitmap);
                }
            }

            return Decode(path, forceAnalyzeBitmap, orientation, out origin);
        }

        private SKBitmap? GetBitmap(string path, bool cropWhitespace, bool autoOrient, ImageOrientation? orientation)
        {
            if (autoOrient)
            {
                var bitmap = GetBitmap(path, cropWhitespace, true, orientation, out var origin);

                if (bitmap != null && origin != SKEncodedOrigin.TopLeft)
                {
                    using (bitmap)
                    {
                        return OrientImage(bitmap, origin);
                    }
                }

                return bitmap;
            }

            return GetBitmap(path, cropWhitespace, false, orientation, out _);
        }

        private SKBitmap OrientImage(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    {
                        var rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                        using (var surface = new SKCanvas(rotated))
                        {
                            surface.Translate(rotated.Width, 0);
                            surface.Scale(-1, 1);
                            surface.DrawBitmap(bitmap, 0, 0);
                        }

                        return rotated;
                    }

                case SKEncodedOrigin.BottomRight:
                    {
                        var rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                        using (var surface = new SKCanvas(rotated))
                        {
                            float px = (float)bitmap.Width / 2;
                            float py = (float)bitmap.Height / 2;

                            surface.RotateDegrees(180, px, py);
                            surface.DrawBitmap(bitmap, 0, 0);
                        }

                        return rotated;
                    }

                case SKEncodedOrigin.BottomLeft:
                    {
                        var rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                        using (var surface = new SKCanvas(rotated))
                        {
                            float px = (float)bitmap.Width / 2;

                            float py = (float)bitmap.Height / 2;

                            surface.Translate(rotated.Width, 0);
                            surface.Scale(-1, 1);

                            surface.RotateDegrees(180, px, py);
                            surface.DrawBitmap(bitmap, 0, 0);
                        }

                        return rotated;
                    }

                case SKEncodedOrigin.LeftTop:
                    {
                        // TODO: Remove dual canvases, had trouble with flipping
                        using (var rotated = new SKBitmap(bitmap.Height, bitmap.Width))
                        {
                            using (var surface = new SKCanvas(rotated))
                            {
                                surface.Translate(rotated.Width, 0);

                                surface.RotateDegrees(90);

                                surface.DrawBitmap(bitmap, 0, 0);
                            }

                            var flippedBitmap = new SKBitmap(rotated.Width, rotated.Height);
                            using (var flippedCanvas = new SKCanvas(flippedBitmap))
                            {
                                flippedCanvas.Translate(flippedBitmap.Width, 0);
                                flippedCanvas.Scale(-1, 1);
                                flippedCanvas.DrawBitmap(rotated, 0, 0);
                            }

                            return flippedBitmap;
                        }
                    }

                case SKEncodedOrigin.RightTop:
                    {
                        var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                        using (var surface = new SKCanvas(rotated))
                        {
                            surface.Translate(rotated.Width, 0);
                            surface.RotateDegrees(90);
                            surface.DrawBitmap(bitmap, 0, 0);
                        }

                        return rotated;
                    }

                case SKEncodedOrigin.RightBottom:
                    {
                        // TODO: Remove dual canvases, had trouble with flipping
                        using (var rotated = new SKBitmap(bitmap.Height, bitmap.Width))
                        {
                            using (var surface = new SKCanvas(rotated))
                            {
                                surface.Translate(0, rotated.Height);
                                surface.RotateDegrees(270);
                                surface.DrawBitmap(bitmap, 0, 0);
                            }

                            var flippedBitmap = new SKBitmap(rotated.Width, rotated.Height);
                            using (var flippedCanvas = new SKCanvas(flippedBitmap))
                            {
                                flippedCanvas.Translate(flippedBitmap.Width, 0);
                                flippedCanvas.Scale(-1, 1);
                                flippedCanvas.DrawBitmap(rotated, 0, 0);
                            }

                            return flippedBitmap;
                        }
                    }

                case SKEncodedOrigin.LeftBottom:
                    {
                        var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                        using (var surface = new SKCanvas(rotated))
                        {
                            surface.Translate(0, rotated.Height);
                            surface.RotateDegrees(270);
                            surface.DrawBitmap(bitmap, 0, 0);
                        }

                        return rotated;
                    }

                default: return bitmap;
            }
        }

        /// <inheritdoc/>
        public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            if (inputPath.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(inputPath));
            }

            if (outputPath.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(outputPath));
            }

            var skiaOutputFormat = GetImageFormat(selectedOutputFormat);

            var hasBackgroundColor = !string.IsNullOrWhiteSpace(options.BackgroundColor);
            var hasForegroundColor = !string.IsNullOrWhiteSpace(options.ForegroundLayer);
            var blur = options.Blur ?? 0;
            var hasIndicator = options.AddPlayedIndicator || options.UnplayedCount.HasValue || !options.PercentPlayed.Equals(0);

            using (var bitmap = GetBitmap(inputPath, options.CropWhiteSpace, autoOrient, orientation))
            {
                if (bitmap == null)
                {
                    throw new InvalidDataException($"Skia unable to read image {inputPath}");
                }

                var originalImageSize = new ImageDimensions(bitmap.Width, bitmap.Height);

                if (!options.CropWhiteSpace
                    && options.HasDefaultOptions(inputPath, originalImageSize)
                    && !autoOrient)
                {
                    // Just spit out the original file if all the options are default
                    return inputPath;
                }

                var newImageSize = ImageHelper.GetNewImageSize(options, originalImageSize);

                var width = newImageSize.Width;
                var height = newImageSize.Height;

                using (var resizedBitmap = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType))
                {
                    // scale image
                    bitmap.ScalePixels(resizedBitmap, SKFilterQuality.High);

                    // If all we're doing is resizing then we can stop now
                    if (!hasBackgroundColor && !hasForegroundColor && blur == 0 && !hasIndicator)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        using (var outputStream = new SKFileWStream(outputPath))
                        using (var pixmap = new SKPixmap(new SKImageInfo(width, height), resizedBitmap.GetPixels()))
                        {
                            pixmap.Encode(outputStream, skiaOutputFormat, quality);
                            return outputPath;
                        }
                    }

                    // create bitmap to use for canvas drawing used to draw into bitmap
                    using (var saveBitmap = new SKBitmap(width, height)) // , bitmap.ColorType, bitmap.AlphaType))
                    using (var canvas = new SKCanvas(saveBitmap))
                    {
                        // set background color if present
                        if (hasBackgroundColor)
                        {
                            canvas.Clear(SKColor.Parse(options.BackgroundColor));
                        }

                        // Add blur if option is present
                        if (blur > 0)
                        {
                            // create image from resized bitmap to apply blur
                            using (var paint = new SKPaint())
                            using (var filter = SKImageFilter.CreateBlur(blur, blur))
                            {
                                paint.ImageFilter = filter;
                                canvas.DrawBitmap(resizedBitmap, SKRect.Create(width, height), paint);
                            }
                        }
                        else
                        {
                            // draw resized bitmap onto canvas
                            canvas.DrawBitmap(resizedBitmap, SKRect.Create(width, height));
                        }

                        // If foreground layer present then draw
                        if (hasForegroundColor)
                        {
                            if (!double.TryParse(options.ForegroundLayer, out double opacity))
                            {
                                opacity = .4;
                            }

                            canvas.DrawColor(new SKColor(0, 0, 0, (byte)((1 - opacity) * 0xFF)), SKBlendMode.SrcOver);
                        }

                        if (hasIndicator)
                        {
                            DrawIndicator(canvas, width, height, options);
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        using (var outputStream = new SKFileWStream(outputPath))
                        {
                            using (var pixmap = new SKPixmap(new SKImageInfo(width, height), saveBitmap.GetPixels()))
                            {
                                pixmap.Encode(outputStream, skiaOutputFormat, quality);
                            }
                        }
                    }
                }
            }

            return outputPath;
        }

        /// <inheritdoc/>
        public void CreateImageCollage(ImageCollageOptions options)
        {
            double ratio = (double)options.Width / options.Height;

            if (ratio >= 1.4)
            {
                new StripCollageBuilder(this).BuildThumbCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
            else if (ratio >= .9)
            {
                new StripCollageBuilder(this).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
            else
            {
                // TODO: Create Poster collage capability
                new StripCollageBuilder(this).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
        }

        private void DrawIndicator(SKCanvas canvas, int imageWidth, int imageHeight, ImageProcessingOptions options)
        {
            try
            {
                var currentImageSize = new ImageDimensions(imageWidth, imageHeight);

                if (options.AddPlayedIndicator)
                {
                    PlayedIndicatorDrawer.DrawPlayedIndicator(canvas, currentImageSize);
                }
                else if (options.UnplayedCount.HasValue)
                {
                    UnplayedCountIndicator.DrawUnplayedCountIndicator(canvas, currentImageSize, options.UnplayedCount.Value);
                }

                if (options.PercentPlayed > 0)
                {
                    PercentPlayedDrawer.Process(canvas, currentImageSize, options.PercentPlayed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error drawing indicator overlay");
            }
        }
    }
}
