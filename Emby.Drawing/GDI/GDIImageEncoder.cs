using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using CommonIO;
using ImageFormat = MediaBrowser.Model.Drawing.ImageFormat;

namespace Emby.Drawing.GDI
{
    public class GDIImageEncoder : IImageEncoder
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public GDIImageEncoder(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;

            LogInfo();
        }

        private void LogInfo()
        {
            _logger.Info("GDIImageEncoder starting");
            using (var stream = GetType().Assembly.GetManifestResourceStream(GetType().Namespace + ".empty.png"))
            {
                using (var img = Image.FromStream(stream))
                {

                }
            }
            _logger.Info("GDIImageEncoder started");
        }

        public string[] SupportedInputFormats
        {
            get
            {
                return new[]
                {
                    "png",
                    "jpeg",
                    "jpg",
                    "gif",
                    "bmp"
                };
            }
        }

        public ImageFormat[] SupportedOutputFormats
        {
            get
            {
                return new[] { ImageFormat.Gif, ImageFormat.Jpg, ImageFormat.Png };
            }
        }

        public ImageSize GetImageSize(string path)
        {
            using (var image = Image.FromFile(path))
            {
                return new ImageSize
                {
                    Width = image.Width,
                    Height = image.Height
                };
            }
        }

        public void CropWhiteSpace(string inputPath, string outputPath)
        {
            using (var image = (Bitmap)Image.FromFile(inputPath))
            {
                using (var croppedImage = image.CropWhitespace())
                {
                    _fileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

                    using (var outputStream = _fileSystem.GetFileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                    {
                        croppedImage.Save(System.Drawing.Imaging.ImageFormat.Png, outputStream, 100);
                    }
                }
            }
        }

        public void EncodeImage(string inputPath, string cacheFilePath, bool autoOrient, int width, int height, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            var hasPostProcessing = !string.IsNullOrEmpty(options.BackgroundColor) || options.UnplayedCount.HasValue || options.AddPlayedIndicator || options.PercentPlayed > 0;

            using (var originalImage = Image.FromFile(inputPath))
            {
                var newWidth = Convert.ToInt32(width);
                var newHeight = Convert.ToInt32(height);

                // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
                // Also, Webp only supports Format32bppArgb and Format32bppRgb
                var pixelFormat = selectedOutputFormat == ImageFormat.Webp
                    ? PixelFormat.Format32bppArgb
                    : PixelFormat.Format32bppPArgb;

                using (var thumbnail = new Bitmap(newWidth, newHeight, pixelFormat))
                {
                    // Mono throw an exeception if assign 0 to SetResolution
                    if (originalImage.HorizontalResolution > 0 && originalImage.VerticalResolution > 0)
                    {
                        // Preserve the original resolution
                        thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
                    }

                    using (var thumbnailGraph = Graphics.FromImage(thumbnail))
                    {
                        thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                        thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                        thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        thumbnailGraph.CompositingMode = !hasPostProcessing ?
                            CompositingMode.SourceCopy :
                            CompositingMode.SourceOver;

                        SetBackgroundColor(thumbnailGraph, options);

                        thumbnailGraph.DrawImage(originalImage, 0, 0, newWidth, newHeight);

                        DrawIndicator(thumbnailGraph, newWidth, newHeight, options);

                        var outputFormat = GetOutputFormat(originalImage, selectedOutputFormat);

                        _fileSystem.CreateDirectory(Path.GetDirectoryName(cacheFilePath));

                        // Save to the cache location
                        using (var cacheFileStream = _fileSystem.GetFileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                        {
                            // Save to the memory stream
                            thumbnail.Save(outputFormat, cacheFileStream, quality);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Sets the color of the background.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="options">The options.</param>
        private void SetBackgroundColor(Graphics graphics, ImageProcessingOptions options)
        {
            var color = options.BackgroundColor;

            if (!string.IsNullOrEmpty(color))
            {
                Color drawingColor;

                try
                {
                    drawingColor = ColorTranslator.FromHtml(color);
                }
                catch
                {
                    drawingColor = ColorTranslator.FromHtml("#" + color);
                }

                graphics.Clear(drawingColor);
            }
        }

        /// <summary>
        /// Draws the indicator.
        /// </summary>
        /// <param name="graphics">The graphics.</param>
        /// <param name="imageWidth">Width of the image.</param>
        /// <param name="imageHeight">Height of the image.</param>
        /// <param name="options">The options.</param>
        private void DrawIndicator(Graphics graphics, int imageWidth, int imageHeight, ImageProcessingOptions options)
        {
            if (!options.AddPlayedIndicator && !options.UnplayedCount.HasValue && options.PercentPlayed.Equals(0))
            {
                return;
            }

            try
            {
                if (options.AddPlayedIndicator)
                {
                    var currentImageSize = new Size(imageWidth, imageHeight);

                    new PlayedIndicatorDrawer().DrawPlayedIndicator(graphics, currentImageSize);
                }
                else if (options.UnplayedCount.HasValue)
                {
                    var currentImageSize = new Size(imageWidth, imageHeight);

                    new UnplayedCountIndicator().DrawUnplayedCountIndicator(graphics, currentImageSize, options.UnplayedCount.Value);
                }

                if (options.PercentPlayed > 0)
                {
                    var currentImageSize = new Size(imageWidth, imageHeight);

                    new PercentPlayedDrawer().Process(graphics, currentImageSize, options.PercentPlayed);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error drawing indicator overlay", ex);
            }
        }

        /// <summary>
        /// Gets the output format.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="outputFormat">The output format.</param>
        /// <returns>ImageFormat.</returns>
        private System.Drawing.Imaging.ImageFormat GetOutputFormat(Image image, ImageFormat outputFormat)
        {
            switch (outputFormat)
            {
                case ImageFormat.Bmp:
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                case ImageFormat.Gif:
                    return System.Drawing.Imaging.ImageFormat.Gif;
                case ImageFormat.Jpg:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                case ImageFormat.Png:
                    return System.Drawing.Imaging.ImageFormat.Png;
                default:
                    return image.RawFormat;
            }
        }

        public void CreateImageCollage(ImageCollageOptions options)
        {
            double ratio = options.Width;
            ratio /= options.Height;

            if (ratio >= 1.4)
            {
                DynamicImageHelpers.CreateThumbCollage(options.InputPaths.ToList(), _fileSystem, options.OutputPath, options.Width, options.Height);
            }
            else if (ratio >= .9)
            {
                DynamicImageHelpers.CreateSquareCollage(options.InputPaths.ToList(), _fileSystem, options.OutputPath, options.Width, options.Height);
            }
            else
            {
                DynamicImageHelpers.CreateSquareCollage(options.InputPaths.ToList(), _fileSystem, options.OutputPath, options.Width, options.Width);
            }
        }

        public void Dispose()
        {
        }

        public string Name
        {
            get { return "GDI"; }
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
