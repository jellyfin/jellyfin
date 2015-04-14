using ImageMagickSharp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using System;
using System.IO;

namespace Emby.Drawing.ImageMagick
{
    public class ImageMagickEncoder : IImageEncoder
    {
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;

        public ImageMagickEncoder(ILogger logger, IApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;

            LogImageMagickVersion();
        }

        public string[] SupportedInputFormats
        {
            get
            {
                // Some common file name extensions for RAW picture files include: .cr2, .crw, .dng, .nef, .orf, .rw2, .pef, .arw, .sr2, .srf, and .tif.
                return new[]
                {
                    "tiff", 
                    "jpeg", 
                    "jpg", 
                    "png", 
                    "aiff", 
                    "cr2", 
                    "crw", 
                    "dng", 
                    "nef", 
                    "orf", 
                    "pef", 
                    "arw", 
                    "webp",
                    "gif",
                    "bmp"
                };
            }
        }

        public ImageFormat[] SupportedOutputFormats
        {
            get
            {
                if (_webpAvailable)
                {
                    return new[] { ImageFormat.Webp, ImageFormat.Gif, ImageFormat.Jpg, ImageFormat.Png };
                }
                return new[] { ImageFormat.Gif, ImageFormat.Jpg, ImageFormat.Png };
            }
        }

        private void LogImageMagickVersion()
        {
            _logger.Info("ImageMagick version: " + Wand.VersionString);
            TestWebp();
        }

        private bool _webpAvailable = true;
        private void TestWebp()
        {
            try
            {
                var tmpPath = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid() + ".webp");
                Directory.CreateDirectory(Path.GetDirectoryName(tmpPath));

                using (var wand = new MagickWand(1, 1, new PixelWand("none", 1)))
                {
                    wand.SaveImage(tmpPath);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading webp: ", ex);
                _webpAvailable = false;
            }
        }

        public void CropWhiteSpace(string inputPath, string outputPath)
        {
            CheckDisposed();

            using (var wand = new MagickWand(inputPath))
            {
                wand.CurrentImage.TrimImage(10);
                wand.SaveImage(outputPath);
            }
        }

        public ImageSize GetImageSize(string path)
        {
            CheckDisposed();

            using (var wand = new MagickWand())
            {
                wand.PingImage(path);
                var img = wand.CurrentImage;

                return new ImageSize
                {
                    Width = img.Width,
                    Height = img.Height
                };
            }
        }

        public void EncodeImage(string inputPath, string outputPath, int width, int height, int quality, ImageProcessingOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.BackgroundColor))
            {
                using (var originalImage = new MagickWand(inputPath))
                {
                    originalImage.CurrentImage.ResizeImage(width, height);

                    DrawIndicator(originalImage, width, height, options);

                    originalImage.CurrentImage.CompressionQuality = quality;

                    originalImage.SaveImage(outputPath);
                }
            }
            else
            {
                using (var wand = new MagickWand(width, height, options.BackgroundColor))
                {
                    using (var originalImage = new MagickWand(inputPath))
                    {
                        originalImage.CurrentImage.ResizeImage(width, height);

                        wand.CurrentImage.CompositeImage(originalImage, CompositeOperator.OverCompositeOp, 0, 0);
                        DrawIndicator(wand, width, height, options);

                        wand.CurrentImage.CompressionQuality = quality;

                        wand.SaveImage(outputPath);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the indicator.
        /// </summary>
        /// <param name="wand">The wand.</param>
        /// <param name="imageWidth">Width of the image.</param>
        /// <param name="imageHeight">Height of the image.</param>
        /// <param name="options">The options.</param>
        private void DrawIndicator(MagickWand wand, int imageWidth, int imageHeight, ImageProcessingOptions options)
        {
            if (!options.AddPlayedIndicator && !options.UnplayedCount.HasValue && options.PercentPlayed.Equals(0))
            {
                return;
            }

            try
            {
                if (options.AddPlayedIndicator)
                {
                    var currentImageSize = new ImageSize(imageWidth, imageHeight);

                    new PlayedIndicatorDrawer(_appPaths).DrawPlayedIndicator(wand, currentImageSize);
                }
                else if (options.UnplayedCount.HasValue)
                {
                    var currentImageSize = new ImageSize(imageWidth, imageHeight);

                    new UnplayedCountIndicator(_appPaths).DrawUnplayedCountIndicator(wand, currentImageSize, options.UnplayedCount.Value);
                }

                if (options.PercentPlayed > 0)
                {
                    new PercentPlayedDrawer().Process(wand, options.PercentPlayed);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error drawing indicator overlay", ex);
            }
        }

        public void CreateImageCollage(ImageCollageOptions options)
        {
            double ratio = options.Width;
            ratio /= options.Height;

            if (ratio >= 1.4)
            {
                new StripCollageBuilder(_appPaths).BuildThumbCollage(options.InputPaths, options.OutputPath, options.Width, options.Height, options.Text);
            }
            else if (ratio >= .9)
            {
                new StripCollageBuilder(_appPaths).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height, options.Text);
            }
            else
            {
                new StripCollageBuilder(_appPaths).BuildPosterCollage(options.InputPaths, options.OutputPath, options.Width, options.Height, options.Text);
            }
        }

        public string Name
        {
            get { return "ImageMagick"; }
        }

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            Wand.CloseEnvironment();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
