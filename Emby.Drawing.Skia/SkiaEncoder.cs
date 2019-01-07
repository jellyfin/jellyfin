using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Controller.Extensions;
using System.Globalization;
using MediaBrowser.Model.Globalization;

namespace Emby.Drawing.Skia
{
    public class SkiaEncoder : IImageEncoder
    {
        private readonly ILogger _logger;
        private static IApplicationPaths _appPaths;
        private readonly Func<IHttpClient> _httpClientFactory;
        private readonly IFileSystem _fileSystem;
        private static ILocalizationManager _localizationManager;

        public SkiaEncoder(ILogger logger, IApplicationPaths appPaths, Func<IHttpClient> httpClientFactory, IFileSystem fileSystem, ILocalizationManager localizationManager)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;
            _localizationManager = localizationManager;

            LogVersion();
        }

        public string[] SupportedInputFormats
        {
            get
            {
                // Some common file name extensions for RAW picture files include: .cr2, .crw, .dng, .nef, .orf, .rw2, .pef, .arw, .sr2, .srf, and .tif.
                return new[]
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

                    // TODO
                    // Are all of these supported? https://github.com/google/skia/blob/master/infra/bots/recipes/test.py#L454

                    // working on windows at least
                    "cr2",
                    "nef",
                    "arw"
                };
            }
        }

        public ImageFormat[] SupportedOutputFormats
        {
            get
            {
                return new[] { ImageFormat.Webp, ImageFormat.Jpg, ImageFormat.Png };
            }
        }

        private void LogVersion()
        {
            // test an operation that requires the native library
            SKPMColor.PreMultiply(SKColors.Black);

            _logger.LogInformation("SkiaSharp version: " + GetVersion());
        }

        public static string GetVersion()
        {
            return typeof(SKBitmap).GetTypeInfo().Assembly.GetName().Version.ToString();
        }

        private static bool IsTransparent(SKColor color)
        {

            return (color.Red == 255 && color.Green == 255 && color.Blue == 255) || color.Alpha == 0;
        }

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
                    topmost = row + 1;
                else break;
            }

            int bottommost = bitmap.Height;
            for (int row = bitmap.Height - 1; row >= 0; --row)
            {
                if (IsTransparentRow(bitmap, row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = bitmap.Width;
            for (int col = 0; col < bitmap.Width; ++col)
            {
                if (IsTransparentColumn(bitmap, col))
                    leftmost = col + 1;
                else
                    break;
            }

            for (int col = bitmap.Width - 1; col >= 0; --col)
            {
                if (IsTransparentColumn(bitmap, col))
                    rightmost = col;
                else
                    break;
            }

            var newRect = SKRectI.Create(leftmost, topmost, rightmost - leftmost, bottommost - topmost);

            using (var image = SKImage.FromBitmap(bitmap))
            using (var subset = image.Subset(newRect))
            {
                return SKBitmap.FromImage(subset);
            }
        }

        public ImageSize GetImageSize(string path)
        {
            using (var s = new SKFileStream(path))
            using (var codec = SKCodec.Create(s))
            {
                var info = codec.Info;

                return new ImageSize
                {
                    Width = info.Width,
                    Height = info.Height
                };
            }
        }

        private static bool HasDiacritics(string text)
        {
            return !String.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);
        }

        private static bool RequiresSpecialCharacterHack(string path)
        {
            if (_localizationManager.HasUnicodeCategory(path, UnicodeCategory.OtherLetter))
            {
                return true;
            }

            if (HasDiacritics(path))
            {
                return true;
            }

            return false;
        }

        private static string NormalizePath(string path, IFileSystem fileSystem)
        {
            if (!RequiresSpecialCharacterHack(path))
            {
                return path;
            }

            var tempPath = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + Path.GetExtension(path) ?? string.Empty);

            fileSystem.CreateDirectory(fileSystem.GetDirectoryName(tempPath));
            fileSystem.CopyFile(path, tempPath, true);

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

        private static string[] TransparentImageTypes = new string[] { ".png", ".gif", ".webp" };
        internal static SKBitmap Decode(string path, bool forceCleanBitmap, IFileSystem fileSystem, ImageOrientation? orientation, out SKEncodedOrigin origin)
        {
            if (!fileSystem.FileExists(path))
            {
                throw new FileNotFoundException("File not found", path);
            }

            var requiresTransparencyHack = TransparentImageTypes.Contains(Path.GetExtension(path) ?? string.Empty);

            if (requiresTransparencyHack || forceCleanBitmap)
            {
                using (var stream = new SKFileStream(NormalizePath(path, fileSystem)))
                using (var codec = SKCodec.Create(stream))
                {
                    if (codec == null)
                    {
                        origin = GetSKEncodedOrigin(orientation);
                        return null;
                    }

                    // create the bitmap
                    var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, !requiresTransparencyHack);

                    if (bitmap != null)
                    {
                        // decode
                        codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                        origin = codec.EncodedOrigin;
                    }
                    else
                    {
                        origin = GetSKEncodedOrigin(orientation);
                    }

                    return bitmap;
                }
            }

            var resultBitmap = SKBitmap.Decode(NormalizePath(path, fileSystem));

            if (resultBitmap == null)
            {
                return Decode(path, true, fileSystem, orientation, out origin);
            }

            // If we have to resize these they often end up distorted
            if (resultBitmap.ColorType == SKColorType.Gray8)
            {
                using (resultBitmap)
                {
                    return Decode(path, true, fileSystem, orientation, out origin);
                }
            }

            origin = SKEncodedOrigin.TopLeft;
            return resultBitmap;
        }

        private SKBitmap GetBitmap(string path, bool cropWhitespace, bool forceAnalyzeBitmap, ImageOrientation? orientation, out SKEncodedOrigin origin)
        {
            if (cropWhitespace)
            {
                using (var bitmap = Decode(path, forceAnalyzeBitmap, _fileSystem, orientation, out origin))
                {
                    return CropWhiteSpace(bitmap);
                }
            }

            return Decode(path, forceAnalyzeBitmap, _fileSystem, orientation, out origin);
        }

        private SKBitmap GetBitmap(string path, bool cropWhitespace, bool autoOrient, ImageOrientation? orientation)
        {
            SKEncodedOrigin origin;

            if (autoOrient)
            {
                var bitmap = GetBitmap(path, cropWhitespace, true, orientation, out origin);

                if (bitmap != null)
                {
                    if (origin != SKEncodedOrigin.TopLeft)
                    {
                        using (bitmap)
                        {
                            return OrientImage(bitmap, origin);
                        }
                    }
                }

                return bitmap;
            }

            return GetBitmap(path, cropWhitespace, false, orientation, out origin);
        }

        private SKBitmap OrientImage(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            //var transformations = {
            //    2: { rotate: 0, flip: true},
            //    3: { rotate: 180, flip: false},
            //    4: { rotate: 180, flip: true},
            //    5: { rotate: 90, flip: true},
            //    6: { rotate: 90, flip: false},
            //    7: { rotate: 270, flip: true},
            //    8: { rotate: 270, flip: false},
            //}

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
                            float px = bitmap.Width;
                            px /= 2;

                            float py = bitmap.Height;
                            py /= 2;

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
                            float px = bitmap.Width;
                            px /= 2;

                            float py = bitmap.Height;
                            py /= 2;

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

                default:
                    return bitmap;
            }
        }

        public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentNullException("inputPath");
            }
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentNullException("outputPath");
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
                    throw new ArgumentOutOfRangeException(string.Format("Skia unable to read image {0}", inputPath));
                }

                //_logger.LogInformation("Color type {0}", bitmap.Info.ColorType);

                var originalImageSize = new ImageSize(bitmap.Width, bitmap.Height);

                if (!options.CropWhiteSpace && options.HasDefaultOptions(inputPath, originalImageSize) && !autoOrient)
                {
                    // Just spit out the original file if all the options are default
                    return inputPath;
                }

                var newImageSize = ImageHelper.GetNewImageSize(options, originalImageSize);

                var width = Convert.ToInt32(Math.Round(newImageSize.Width));
                var height = Convert.ToInt32(Math.Round(newImageSize.Height));

                using (var resizedBitmap = new SKBitmap(width, height))//, bitmap.ColorType, bitmap.AlphaType))
                {
                    // scale image
                    bitmap.ScalePixels(resizedBitmap, SKFilterQuality.High);

                    // If all we're doing is resizing then we can stop now
                    if (!hasBackgroundColor && !hasForegroundColor && blur == 0 && !hasIndicator)
                    {
                        _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(outputPath));
                        using (var outputStream = new SKFileWStream(outputPath))
                        {
                            using (var pixmap = new SKPixmap(new SKImageInfo(width, height), resizedBitmap.GetPixels()))
                            {
                                pixmap.Encode(outputStream, skiaOutputFormat, quality);
                                return outputPath;
                            }
                        }
                    }

                    // create bitmap to use for canvas drawing used to draw into bitmap
                    using (var saveBitmap = new SKBitmap(width, height))//, bitmap.ColorType, bitmap.AlphaType))
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
                            if (!Double.TryParse(options.ForegroundLayer, out double opacity))
                            {
                                opacity = .4;
                            }

                            canvas.DrawColor(new SKColor(0, 0, 0, (Byte)((1 - opacity) * 0xFF)), SKBlendMode.SrcOver);
                        }

                        if (hasIndicator)
                        {
                            DrawIndicator(canvas, width, height, options);
                        }

                        _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(outputPath));
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

        public void CreateImageCollage(ImageCollageOptions options)
        {
            double ratio = options.Width;
            ratio /= options.Height;

            if (ratio >= 1.4)
            {
                new StripCollageBuilder(_appPaths, _fileSystem).BuildThumbCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
            else if (ratio >= .9)
            {
                new StripCollageBuilder(_appPaths, _fileSystem).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
            else
            {
                // @todo create Poster collage capability
                new StripCollageBuilder(_appPaths, _fileSystem).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
        }

        private void DrawIndicator(SKCanvas canvas, int imageWidth, int imageHeight, ImageProcessingOptions options)
        {
            try
            {
                var currentImageSize = new ImageSize(imageWidth, imageHeight);

                if (options.AddPlayedIndicator)
                {
                    new PlayedIndicatorDrawer(_appPaths, _httpClientFactory(), _fileSystem).DrawPlayedIndicator(canvas, currentImageSize);
                }
                else if (options.UnplayedCount.HasValue)
                {
                    new UnplayedCountIndicator(_appPaths, _httpClientFactory(), _fileSystem).DrawUnplayedCountIndicator(canvas, currentImageSize, options.UnplayedCount.Value);
                }

                if (options.PercentPlayed > 0)
                {
                    new PercentPlayedDrawer().Process(canvas, currentImageSize, options.PercentPlayed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error drawing indicator overlay");
            }
        }

        public string Name
        {
            get { return "Skia"; }
        }

        public bool SupportsImageCollageCreation
        {
            get { return true; }
        }

        public bool SupportsImageEncoding
        {
            get { return true; }
        }
    }
}
