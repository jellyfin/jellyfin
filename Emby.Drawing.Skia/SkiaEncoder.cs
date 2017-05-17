using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Emby.Drawing.Skia
{
    public class SkiaEncoder : IImageEncoder
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly Func<IHttpClient> _httpClientFactory;
        private readonly IFileSystem _fileSystem;

        public SkiaEncoder(ILogger logger, IApplicationPaths appPaths, Func<IHttpClient> httpClientFactory, IFileSystem fileSystem)
        {
            _logger = logger;
            _appPaths = appPaths;
            _httpClientFactory = httpClientFactory;
            _fileSystem = fileSystem;

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
                    "wbmp"
                };
            }
        }

        public ImageFormat[] SupportedOutputFormats
        {
            get
            {
                return new[] { ImageFormat.Webp, ImageFormat.Gif, ImageFormat.Jpg, ImageFormat.Png, ImageFormat.Bmp };
            }
        }

        private void LogVersion()
        {
            _logger.Info("SkiaSharp version: " + GetVersion());
        }

        public static string GetVersion()
        {
            using (var bitmap = new SKBitmap())
            {
                return typeof(SKBitmap).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }

        private static bool IsWhiteSpace(SKColor color)
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

        private static bool IsAllWhiteRow(SKBitmap bmp, int row)
        {
            for (var i = 0; i < bmp.Width; ++i)
            {
                if (!IsWhiteSpace(bmp.GetPixel(i, row)))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsAllWhiteColumn(SKBitmap bmp, int col)
        {
            for (var i = 0; i < bmp.Height; ++i)
            {
                if (!IsWhiteSpace(bmp.GetPixel(col, i)))
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
                if (IsAllWhiteRow(bitmap, row))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = bitmap.Height - 1; row >= 0; --row)
            {
                if (IsAllWhiteRow(bitmap, row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < bitmap.Width; ++col)
            {
                if (IsAllWhiteColumn(bitmap, col))
                    leftmost = col;
                else
                    break;
            }

            for (int col = bitmap.Width - 1; col >= 0; --col)
            {
                if (IsAllWhiteColumn(bitmap, col))
                    rightmost = col;
                else
                    break;
            }

            var newRect = SKRectI.Create(leftmost, topmost, rightmost - leftmost, bottommost - topmost);

            using (var image = SKImage.FromBitmap(bitmap))
            {
                using (var subset = image.Subset(newRect))
                {
                    return SKBitmap.FromImage(subset);
                    //using (var data = subset.Encode(StripCollageBuilder.GetEncodedFormat(outputPath), 90))
                    //{
                    //    using (var fileStream = _fileSystem.GetFileStream(outputPath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                    //    {
                    //        data.AsStream().CopyTo(fileStream);
                    //    }
                    //}
                }
            }
        }

        public ImageSize GetImageSize(string path)
        {
            using (var s = new SKFileStream(path))
            {
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
        }

        private string[] TransparentImageTypes = new string[] { ".png", ".gif", ".webp" };
        private SKBitmap Decode(string path, bool forceCleanBitmap = false)
        {
            var requiresTransparencyHack = TransparentImageTypes.Contains(Path.GetExtension(path) ?? string.Empty);

            if (requiresTransparencyHack || forceCleanBitmap)
            {
                using (var stream = new SKFileStream(path))
                {
                    var codec = SKCodec.Create(stream);

                    // create the bitmap
                    var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, !requiresTransparencyHack);
                    // decode
                    codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                    return bitmap;
                }
            }

            var resultBitmap = SKBitmap.Decode(path);

            // If we have to resize these they often end up distorted
            if (resultBitmap.ColorType == SKColorType.Gray8)
            {
                using (resultBitmap)
                {
                    return Decode(path, true);
                }
            }

            return resultBitmap;
        }

        private SKBitmap GetBitmap(string path, bool cropWhitespace)
        {
            if (cropWhitespace)
            {
                using (var bitmap = Decode(path))
                {
                    return CropWhiteSpace(bitmap);
                }
            }

            return Decode(path);
        }

        public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
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

            using (var bitmap = GetBitmap(inputPath, options.CropWhiteSpace))
            {
                if (bitmap == null)
                {
                    throw new Exception(string.Format("Skia unable to read image {0}", inputPath));
                }

                //_logger.Info("Color type {0}", bitmap.Info.ColorType);

                var originalImageSize = new ImageSize(bitmap.Width, bitmap.Height);
                ImageHelper.SaveImageSize(inputPath, dateModified, originalImageSize);

                if (!options.CropWhiteSpace && options.HasDefaultOptions(inputPath, originalImageSize))
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
                    var resizeMethod = SKBitmapResizeMethod.Lanczos3;

                    bitmap.Resize(resizedBitmap, resizeMethod);

                    // If all we're doing is resizing then we can stop now
                    if (!hasBackgroundColor && !hasForegroundColor && blur == 0 && !hasIndicator)
                    {
                        using (var outputStream = new SKFileWStream(outputPath))
                        {
                            resizedBitmap.Encode(outputStream, skiaOutputFormat, quality);
                            return outputPath;
                        }
                    }

                    // create bitmap to use for canvas drawing
                    using (var saveBitmap = new SKBitmap(width, height))//, bitmap.ColorType, bitmap.AlphaType))
                    {
                        // create canvas used to draw into bitmap
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
                                using (var paint = new SKPaint())
                                {
                                    // create image from resized bitmap to apply blur
                                    using (var filter = SKImageFilter.CreateBlur(blur, blur))
                                    {
                                        paint.ImageFilter = filter;
                                        canvas.DrawBitmap(resizedBitmap, SKRect.Create(width, height), paint);
                                    }
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
                                Double opacity;
                                if (!Double.TryParse(options.ForegroundLayer, out opacity)) opacity = .4;

                                canvas.DrawColor(new SKColor(0, 0, 0, (Byte)((1 - opacity) * 0xFF)), SKBlendMode.SrcOver);
                            }

                            if (hasIndicator)
                            {
                                DrawIndicator(canvas, width, height, options);
                            }

                            using (var outputStream = new SKFileWStream(outputPath))
                            {
                                saveBitmap.Encode(outputStream, skiaOutputFormat, quality);
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
                    var task = new PlayedIndicatorDrawer(_appPaths, _httpClientFactory(), _fileSystem).DrawPlayedIndicator(canvas, currentImageSize);
                    Task.WaitAll(task);
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
                _logger.ErrorException("Error drawing indicator overlay", ex);
            }
        }

        public string Name
        {
            get { return "Skia"; }
        }

        public void Dispose()
        {
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