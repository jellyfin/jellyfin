using Imazen.WebP;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Drawing
{
    /// <summary>
    /// Class ImageProcessor
    /// </summary>
    public class ImageProcessor : IImageProcessor, IDisposable
    {
        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// The _cached imaged sizes
        /// </summary>
        private readonly ConcurrentDictionary<Guid, ImageSize> _cachedImagedSizes;

        /// <summary>
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IEnumerable<IImageEnhancer> ImageEnhancers { get; private set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IMediaEncoder _mediaEncoder;

        public ImageProcessor(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;

            _saveImageSizeTimer = new Timer(SaveImageSizeCallback, null, Timeout.Infinite, Timeout.Infinite);

            Dictionary<Guid, ImageSize> sizeDictionary;

            try
            {
                sizeDictionary = jsonSerializer.DeserializeFromFile<Dictionary<Guid, ImageSize>>(ImageSizeFile) ??
                    new Dictionary<Guid, ImageSize>();
            }
            catch (FileNotFoundException)
            {
                // No biggie
                sizeDictionary = new Dictionary<Guid, ImageSize>();
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error parsing image size cache file", ex);

                sizeDictionary = new Dictionary<Guid, ImageSize>();
            }

            _cachedImagedSizes = new ConcurrentDictionary<Guid, ImageSize>(sizeDictionary);

            LogWebPVersion();
        }

        private string ResizedImageCachePath
        {
            get
            {
                return Path.Combine(_appPaths.ImageCachePath, "resized-images");
            }
        }

        private string EnhancedImageCachePath
        {
            get
            {
                return Path.Combine(_appPaths.ImageCachePath, "enhanced-images");
            }
        }

        private string CroppedWhitespaceImageCachePath
        {
            get
            {
                return Path.Combine(_appPaths.ImageCachePath, "cropped-images");
            }
        }

        public void AddParts(IEnumerable<IImageEnhancer> enhancers)
        {
            ImageEnhancers = enhancers.ToArray();
        }

        public async Task ProcessImage(ImageProcessingOptions options, Stream toStream)
        {
            var file = await ProcessImage(options).ConfigureAwait(false);

            using (var fileStream = _fileSystem.GetFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
            }
        }

        public Model.Drawing.ImageFormat[] GetSupportedImageOutputFormats()
        {
            if (_webpAvailable)
            {
                return new[] { Model.Drawing.ImageFormat.Webp, Model.Drawing.ImageFormat.Gif, Model.Drawing.ImageFormat.Jpg, Model.Drawing.ImageFormat.Png };
            }
            return new[] { Model.Drawing.ImageFormat.Gif, Model.Drawing.ImageFormat.Jpg, Model.Drawing.ImageFormat.Png };
        }

        public async Task<string> ProcessImage(ImageProcessingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var originalImagePath = options.Image.Path;

            if (options.HasDefaultOptions(originalImagePath) && options.Enhancers.Count == 0 && !options.CropWhiteSpace)
            {
                // Just spit out the original file if all the options are default
                return originalImagePath;
            }

            var dateModified = options.Image.DateModified;

            if (options.CropWhiteSpace)
            {
                var tuple = await GetWhitespaceCroppedImage(originalImagePath, dateModified).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            if (options.Enhancers.Count > 0)
            {
                var tuple = await GetEnhancedImage(options.Image, options.Item, options.ImageIndex, options.Enhancers).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            var originalImageSize = GetImageSize(originalImagePath, dateModified);

            // Determine the output size based on incoming parameters
            var newSize = DrawingUtils.Resize(originalImageSize, options.Width, options.Height, options.MaxWidth, options.MaxHeight);

            if (options.HasDefaultOptionsWithoutSize(originalImagePath) && newSize.Equals(originalImageSize) && options.Enhancers.Count == 0)
            {
                // Just spit out the original file if the new size equals the old
                return originalImagePath;
            }

            var quality = options.Quality ?? 90;

            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality, dateModified, options.OutputFormat, options.AddPlayedIndicator, options.PercentPlayed, options.UnplayedCount, options.BackgroundColor);

            var semaphore = GetLock(cacheFilePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of lock contention
            try
            {
                if (File.Exists(cacheFilePath))
                {
                    semaphore.Release();
                    return cacheFilePath;
                }
            }
            catch
            {
                semaphore.Release();
                throw;
            }

            try
            {
                var hasPostProcessing = !string.IsNullOrEmpty(options.BackgroundColor) || options.UnplayedCount.HasValue || options.AddPlayedIndicator || options.PercentPlayed > 0;

                using (var fileStream = _fileSystem.GetFileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    // Copy to memory stream to avoid Image locking file
                    using (var memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                        using (var originalImage = Image.FromStream(memoryStream, true, false))
                        {
                            var newWidth = Convert.ToInt32(newSize.Width);
                            var newHeight = Convert.ToInt32(newSize.Height);

                            var selectedOutputFormat = options.OutputFormat;

                            _logger.Debug("Processing image to {0}", selectedOutputFormat);

                            // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
                            // Also, Webp only supports Format32bppArgb and Format32bppRgb
                            var pixelFormat = selectedOutputFormat == Model.Drawing.ImageFormat.Webp
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

                                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath));

                                    // Save to the cache location
                                    using (var cacheFileStream = _fileSystem.GetFileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                                    {
                                        if (selectedOutputFormat == Model.Drawing.ImageFormat.Webp)
                                        {
                                            SaveToWebP(thumbnail, cacheFileStream, quality);
                                        }
                                        else
                                        {
                                            // Save to the memory stream
                                            thumbnail.Save(outputFormat, cacheFileStream, quality);
                                        }
                                    }

                                    return cacheFilePath;
                                }
                            }

                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void SaveToWebP(Bitmap thumbnail, Stream toStream, int quality)
        {
            new SimpleEncoder().Encode(thumbnail, toStream, quality);
        }

        private bool _webpAvailable = true;
        private void LogWebPVersion()
        {
            try
            {
                _logger.Info("libwebp version: " + SimpleEncoder.GetEncoderVersion());
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading libwebp: ", ex);
                _webpAvailable = false;
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

                if (options.PercentPlayed >= 0)
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
        private System.Drawing.Imaging.ImageFormat GetOutputFormat(Image image, Model.Drawing.ImageFormat outputFormat)
        {
            switch (outputFormat)
            {
                case Model.Drawing.ImageFormat.Bmp:
                    return System.Drawing.Imaging.ImageFormat.Bmp;
                case Model.Drawing.ImageFormat.Gif:
                    return System.Drawing.Imaging.ImageFormat.Gif;
                case Model.Drawing.ImageFormat.Jpg:
                    return System.Drawing.Imaging.ImageFormat.Jpeg;
                case Model.Drawing.ImageFormat.Png:
                    return System.Drawing.Imaging.ImageFormat.Png;
                default:
                    return image.RawFormat;
            }
        }

        /// <summary>
        /// Crops whitespace from an image, caches the result, and returns the cached path
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns>System.String.</returns>
        private async Task<Tuple<string, DateTime>> GetWhitespaceCroppedImage(string originalImagePath, DateTime dateModified)
        {
            var name = originalImagePath;
            name += "datemodified=" + dateModified.Ticks;

            var croppedImagePath = GetCachePath(CroppedWhitespaceImageCachePath, name, Path.GetExtension(originalImagePath));

            var semaphore = GetLock(croppedImagePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of contention
            if (File.Exists(croppedImagePath))
            {
                semaphore.Release();
                return new Tuple<string, DateTime>(croppedImagePath, _fileSystem.GetLastWriteTimeUtc(croppedImagePath));
            }

            try
            {
                using (var fileStream = _fileSystem.GetFileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    // Copy to memory stream to avoid Image locking file
                    using (var memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                        using (var originalImage = (Bitmap)Image.FromStream(memoryStream, true, false))
                        {
                            var outputFormat = originalImage.RawFormat;

                            using (var croppedImage = originalImage.CropWhitespace())
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(croppedImagePath));

                                using (var outputStream = _fileSystem.GetFileStream(croppedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                                {
                                    croppedImage.Save(outputFormat, outputStream, 100);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // We have to have a catch-all here because some of the .net image methods throw a plain old Exception
                _logger.ErrorException("Error cropping image {0}", ex, originalImagePath);

                return new Tuple<string, DateTime>(originalImagePath, dateModified);
            }
            finally
            {
                semaphore.Release();
            }

            return new Tuple<string, DateTime>(croppedImagePath, _fileSystem.GetLastWriteTimeUtc(croppedImagePath));
        }

        /// <summary>
        /// Increment this when indicator drawings change
        /// </summary>
        private const string IndicatorVersion = "2";

        /// <summary>
        /// Gets the cache file path based on a set of parameters
        /// </summary>
        private string GetCacheFilePath(string originalPath, ImageSize outputSize, int quality, DateTime dateModified, Model.Drawing.ImageFormat format, bool addPlayedIndicator, double percentPlayed, int? unwatchedCount, string backgroundColor)
        {
            var filename = originalPath;

            filename += "width=" + outputSize.Width;

            filename += "height=" + outputSize.Height;

            filename += "quality=" + quality;

            filename += "datemodified=" + dateModified.Ticks;

            filename += "f=" + format;

            var hasIndicator = false;

            if (addPlayedIndicator)
            {
                filename += "pl=true";
                hasIndicator = true;
            }

            if (percentPlayed > 0)
            {
                filename += "p=" + percentPlayed;
                hasIndicator = true;
            }

            if (unwatchedCount.HasValue)
            {
                filename += "p=" + unwatchedCount.Value;
                hasIndicator = true;
            }

            if (hasIndicator)
            {
                filename += "iv=" + IndicatorVersion;
            }

            if (!string.IsNullOrEmpty(backgroundColor))
            {
                filename += "b=" + backgroundColor;
            }

            return GetCachePath(ResizedImageCachePath, filename, Path.GetExtension(originalPath));
        }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>ImageSize.</returns>
        public ImageSize GetImageSize(string path)
        {
            return GetImageSize(path, File.GetLastWriteTimeUtc(path));
        }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="imageDateModified">The image date modified.</param>
        /// <returns>ImageSize.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        public ImageSize GetImageSize(string path, DateTime imageDateModified)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var name = path + "datemodified=" + imageDateModified.Ticks;

            ImageSize size;

            var cacheHash = name.GetMD5();

            if (!_cachedImagedSizes.TryGetValue(cacheHash, out size))
            {
                size = GetImageSizeInternal(path);

                _cachedImagedSizes.AddOrUpdate(cacheHash, size, (keyName, oldValue) => size);
            }

            return size;
        }

        /// <summary>
        /// Gets the image size internal.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>ImageSize.</returns>
        private ImageSize GetImageSizeInternal(string path)
        {
            var size = ImageHeader.GetDimensions(path, _logger, _fileSystem);

            StartSaveImageSizeTimer();

            return new ImageSize { Width = size.Width, Height = size.Height };
        }

        private readonly Timer _saveImageSizeTimer;
        private const int SaveImageSizeTimeout = 5000;
        private readonly object _saveImageSizeLock = new object();
        private void StartSaveImageSizeTimer()
        {
            _saveImageSizeTimer.Change(SaveImageSizeTimeout, Timeout.Infinite);
        }

        private void SaveImageSizeCallback(object state)
        {
            lock (_saveImageSizeLock)
            {
                try
                {
                    var path = ImageSizeFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    _jsonSerializer.SerializeToFile(_cachedImagedSizes, path);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error saving image size file", ex);
                }
            }
        }

        private string ImageSizeFile
        {
            get
            {
                return Path.Combine(_appPaths.DataPath, "imagesizes.json");
            }
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageCacheTag(IHasImages item, ItemImageInfo image)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            var supportedEnhancers = GetSupportedEnhancers(item, image.Type);

            return GetImageCacheTag(item, image, supportedEnhancers.ToList());
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageCacheTag(IHasImages item, ItemImageInfo image, List<IImageEnhancer> imageEnhancers)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (imageEnhancers == null)
            {
                throw new ArgumentNullException("imageEnhancers");
            }

            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            var originalImagePath = image.Path;
            var dateModified = image.DateModified;
            var imageType = image.Type;

            // Optimization
            if (imageEnhancers.Count == 0)
            {
                return (originalImagePath + dateModified.Ticks).GetMD5().ToString("N");
            }

            // Cache name is created with supported enhancers combined with the last config change so we pick up new config changes
            var cacheKeys = imageEnhancers.Select(i => i.GetConfigurationCacheKey(item, imageType)).ToList();
            cacheKeys.Add(originalImagePath + dateModified.Ticks);

            return string.Join("|", cacheKeys.ToArray()).GetMD5().ToString("N");
        }

        /// <summary>
        /// Gets the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> GetEnhancedImage(IHasImages item, ImageType imageType, int imageIndex)
        {
            var enhancers = GetSupportedEnhancers(item, imageType).ToList();

            var imageInfo = item.GetImageInfo(imageType, imageIndex);

            var result = await GetEnhancedImage(imageInfo, item, imageIndex, enhancers);

            return result.Item1;
        }

        private async Task<Tuple<string, DateTime>> GetEnhancedImage(ItemImageInfo image,
            IHasImages item,
            int imageIndex,
            List<IImageEnhancer> enhancers)
        {
            var originalImagePath = image.Path;
            var dateModified = image.DateModified;
            var imageType = image.Type;

            try
            {
                var cacheGuid = GetImageCacheTag(item, image, enhancers);

                // Enhance if we have enhancers
                var ehnancedImagePath = await GetEnhancedImageInternal(originalImagePath, item, imageType, imageIndex, enhancers, cacheGuid).ConfigureAwait(false);

                // If the path changed update dateModified
                if (!ehnancedImagePath.Equals(originalImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    dateModified = _fileSystem.GetLastWriteTimeUtc(ehnancedImagePath);

                    return new Tuple<string, DateTime>(ehnancedImagePath, dateModified);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error enhancing image", ex);
            }

            return new Tuple<string, DateTime>(originalImagePath, dateModified);
        }

        /// <summary>
        /// Runs an image through the image enhancers, caches the result, and returns the cached path
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="supportedEnhancers">The supported enhancers.</param>
        /// <param name="cacheGuid">The cache unique identifier.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">originalImagePath</exception>
        private async Task<string> GetEnhancedImageInternal(string originalImagePath,
            IHasImages item,
            ImageType imageType,
            int imageIndex,
            IEnumerable<IImageEnhancer> supportedEnhancers,
            string cacheGuid)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            // All enhanced images are saved as png to allow transparency
            var enhancedImagePath = GetCachePath(EnhancedImageCachePath, cacheGuid + ".png");

            var semaphore = GetLock(enhancedImagePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of contention
            if (File.Exists(enhancedImagePath))
            {
                semaphore.Release();
                return enhancedImagePath;
            }

            try
            {
                using (var fileStream = _fileSystem.GetFileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    // Copy to memory stream to avoid Image locking file
                    using (var memoryStream = new MemoryStream())
                    {
                        await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                        memoryStream.Position = 0;

                        var imageStream = new ImageStream
                        {
                            Stream = memoryStream,
                            Format = GetFormat(originalImagePath)
                        };

                        //Pass the image through registered enhancers
                        using (var newImageStream = await ExecuteImageEnhancers(supportedEnhancers, imageStream, item, imageType, imageIndex).ConfigureAwait(false))
                        {
                            var parentDirectory = Path.GetDirectoryName(enhancedImagePath);

                            Directory.CreateDirectory(parentDirectory);

                            // Save as png
                            if (newImageStream.Format == Model.Drawing.ImageFormat.Png)
                            {
                                //And then save it in the cache
                                using (var outputStream = _fileSystem.GetFileStream(enhancedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                                {
                                    await newImageStream.Stream.CopyToAsync(outputStream).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                using (var newImage = Image.FromStream(newImageStream.Stream, true, false))
                                {
                                    //And then save it in the cache
                                    using (var outputStream = _fileSystem.GetFileStream(enhancedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                                    {
                                        newImage.Save(System.Drawing.Imaging.ImageFormat.Png, outputStream, 100);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            return enhancedImagePath;
        }

        private Model.Drawing.ImageFormat GetFormat(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                return Model.Drawing.ImageFormat.Png;
            }
            if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                return Model.Drawing.ImageFormat.Gif;
            }
            if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                return Model.Drawing.ImageFormat.Webp;
            }
            if (string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase))
            {
                return Model.Drawing.ImageFormat.Bmp;
            }

            return Model.Drawing.ImageFormat.Jpg;
        }

        /// <summary>
        /// Executes the image enhancers.
        /// </summary>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <param name="originalImage">The original image.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{EnhancedImage}.</returns>
        private async Task<ImageStream> ExecuteImageEnhancers(IEnumerable<IImageEnhancer> imageEnhancers, ImageStream originalImage, IHasImages item, ImageType imageType, int imageIndex)
        {
            var result = originalImage;

            // Run the enhancers sequentially in order of priority
            foreach (var enhancer in imageEnhancers)
            {
                var typeName = enhancer.GetType().Name;

                try
                {
                    result = await enhancer.EnhanceImageAsync(item, result, imageType, imageIndex).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed enhancing {1}", ex, typeName, item.Name);

                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// The _semaphoreLocks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphoreLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _semaphoreLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Gets the cache path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="uniqueName">Name of the unique.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// path
        /// or
        /// uniqueName
        /// or
        /// fileExtension
        /// </exception>
        public string GetCachePath(string path, string uniqueName, string fileExtension)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (string.IsNullOrEmpty(uniqueName))
            {
                throw new ArgumentNullException("uniqueName");
            }

            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentNullException("fileExtension");
            }

            var filename = uniqueName.GetMD5() + fileExtension;

            return GetCachePath(path, filename);
        }

        /// <summary>
        /// Gets the cache path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// path
        /// or
        /// filename
        /// </exception>
        public string GetCachePath(string path, string filename)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            var prefix = filename.Substring(0, 1);

            path = Path.Combine(path, prefix);

            return Path.Combine(path, filename);
        }

        public IEnumerable<IImageEnhancer> GetSupportedEnhancers(IHasImages item, ImageType imageType)
        {
            return ImageEnhancers.Where(i =>
            {
                try
                {
                    return i.Supports(item, imageType);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in image enhancer: {0}", ex, i.GetType().Name);

                    return false;
                }

            });
        }

        public void Dispose()
        {
            _saveImageSizeTimer.Dispose();
        }
    }
}
