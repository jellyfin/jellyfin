using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using SkiaSharp;
using System;
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
            return typeof(SKCanvas).GetTypeInfo().Assembly.GetName().Version.ToString();
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
                case ImageFormat.Png:
                default:
                    return SKEncodedImageFormat.Png;
            }
        }

        public void CropWhiteSpace(string inputPath, string outputPath)
        {
            CheckDisposed();

            using (var bitmap = SKBitmap.Decode(inputPath))
            {
                // @todo
            }
        }

        public ImageSize GetImageSize(string path)
        {
            CheckDisposed();

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

        public void EncodeImage(string inputPath, string outputPath, bool autoOrient, int width, int height, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            using (var bitmap = SKBitmap.Decode(inputPath))
            {
                using (var resizedBitmap = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType))
                {
                    // scale image
                    bitmap.Resize(resizedBitmap, SKBitmapResizeMethod.Lanczos3);

                    // create bitmap to use for canvas drawing
                    using (var saveBitmap = new SKBitmap(width, height, bitmap.ColorType, bitmap.AlphaType))
                    {
                        // create canvas used to draw into bitmap
                        using (var canvas = new SKCanvas(saveBitmap))
                        {
                            // set background color if present
                            if (!string.IsNullOrWhiteSpace(options.BackgroundColor))
                            {
                                canvas.Clear(SKColor.Parse(options.BackgroundColor));
                            }

                            // Add blur if option is present
                            if (options.Blur > 0)
                            {
                                using (var paint = new SKPaint())
                                {
                                    // create image from resized bitmap to apply blur
                                    using (var filter = SKImageFilter.CreateBlur(5, 5))
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
                            if (!string.IsNullOrWhiteSpace(options.ForegroundLayer))
                            {
                                Double opacity;
                                if (!Double.TryParse(options.ForegroundLayer, out opacity)) opacity = .4;

                                var foregroundColor = String.Format("#{0:X2}000000", (Byte)((1-opacity) * 0xFF));
                                canvas.DrawColor(SKColor.Parse(foregroundColor), SKBlendMode.SrcOver);
                            }

                            DrawIndicator(canvas, width, height, options);

                            using (var outputStream = new SKFileWStream(outputPath))
                            {
                                saveBitmap.Encode(outputStream, GetImageFormat(selectedOutputFormat), quality);
                            }
                        }
                    }
                }
            }
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
                new StripCollageBuilder(_appPaths, _fileSystem).BuildPosterCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
            }
        }

        private void DrawIndicator(SKCanvas canvas, int imageWidth, int imageHeight, ImageProcessingOptions options)
        {
            if (!options.AddPlayedIndicator && !options.UnplayedCount.HasValue && options.PercentPlayed.Equals(0))
            {
                return;
            }

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

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
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
