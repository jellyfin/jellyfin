using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
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

        public ImageProcessor(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
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
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (toStream == null)
            {
                throw new ArgumentNullException("toStream");
            }

            var originalImagePath = options.OriginalImagePath;

            if (options.HasDefaultOptions() && options.Enhancers.Count == 0 && !options.CropWhiteSpace)
            {
                // Just spit out the original file if all the options are default
                using (var fileStream = _fileSystem.GetFileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                    return;
                }
            }

            var dateModified = options.OriginalImageDateModified;

            if (options.CropWhiteSpace)
            {
                var tuple = await GetWhitespaceCroppedImage(originalImagePath, dateModified).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            if (options.Enhancers.Count > 0)
            {
                var tuple = await GetEnhancedImage(originalImagePath, dateModified, options.Item, options.ImageType, options.ImageIndex, options.Enhancers).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            var originalImageSize = GetImageSize(originalImagePath, dateModified);

            // Determine the output size based on incoming parameters
            var newSize = DrawingUtils.Resize(originalImageSize, options.Width, options.Height, options.MaxWidth, options.MaxHeight);

            if (options.HasDefaultOptionsWithoutSize() && newSize.Equals(originalImageSize) && options.Enhancers.Count == 0)
            {
                // Just spit out the original file if the new size equals the old
                using (var fileStream = _fileSystem.GetFileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                    return;
                }
            }

            var quality = options.Quality ?? 90;

            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality, dateModified, options.OutputFormat, options.AddPlayedIndicator, options.PercentPlayed, options.UnplayedCount, options.BackgroundColor);

            try
            {
                using (var fileStream = _fileSystem.GetFileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                    return;
                }
            }
            catch (IOException)
            {
                // Cache file doesn't exist or is currently being written to
            }

            var semaphore = GetLock(cacheFilePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of lock contention
            try
            {
                using (var fileStream = _fileSystem.GetFileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, true))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                    semaphore.Release();
                    return;
                }
            }
            catch (IOException)
            {
                // Cache file doesn't exist or is currently being written to
            }
            catch
            {
                semaphore.Release();
                throw;
            }

            try
            {
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

                            // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
                            using (var thumbnail = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppPArgb))
                            {
                                // Mono throw an exeception if assign 0 to SetResolution
                                if (originalImage.HorizontalResolution >= 0 && originalImage.VerticalResolution >= 0)
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
                                    thumbnailGraph.CompositingMode = string.IsNullOrEmpty(options.BackgroundColor) && !options.UnplayedCount.HasValue && !options.AddPlayedIndicator && !options.PercentPlayed.HasValue ? 
                                        CompositingMode.SourceCopy : 
                                        CompositingMode.SourceOver;

                                    SetBackgroundColor(thumbnailGraph, options);

                                    thumbnailGraph.DrawImage(originalImage, 0, 0, newWidth, newHeight);

                                    DrawIndicator(thumbnailGraph, newWidth, newHeight, options);

                                    var outputFormat = GetOutputFormat(originalImage, options.OutputFormat);

                                    using (var outputMemoryStream = new MemoryStream())
                                    {
                                        // Save to the memory stream
                                        thumbnail.Save(outputFormat, outputMemoryStream, quality);

                                        var bytes = outputMemoryStream.ToArray();

                                        await toStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

                                        // kick off a task to cache the result
                                        CacheResizedImage(cacheFilePath, bytes, semaphore);
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch
            {
                semaphore.Release();

                throw;
            }
        }

        /// <summary>
        /// Caches the resized image.
        /// </summary>
        /// <param name="cacheFilePath">The cache file path.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="semaphore">The semaphore.</param>
        private void CacheResizedImage(string cacheFilePath, byte[] bytes, SemaphoreSlim semaphore)
        {
            Task.Run(async () =>
            {
                try
                {
                    var parentPath = Path.GetDirectoryName(cacheFilePath);

                    Directory.CreateDirectory(parentPath);

                    // Save to the cache location
                    using (var cacheFileStream = _fileSystem.GetFileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        // Save to the filestream
                        await cacheFileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error writing to image cache file {0}", ex, cacheFilePath);
                }
                finally
                {
                    semaphore.Release();
                }
            });
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
            if (!options.AddPlayedIndicator && !options.UnplayedCount.HasValue && !options.PercentPlayed.HasValue)
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

                if (options.PercentPlayed.HasValue)
                {
                    var currentImageSize = new Size(imageWidth, imageHeight);

                    new PercentPlayedDrawer().Process(graphics, currentImageSize, options.PercentPlayed.Value);
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
        private ImageFormat GetOutputFormat(Image image, ImageOutputFormat outputFormat)
        {
            switch (outputFormat)
            {
                case ImageOutputFormat.Bmp:
                    return ImageFormat.Bmp;
                case ImageOutputFormat.Gif:
                    return ImageFormat.Gif;
                case ImageOutputFormat.Jpg:
                    return ImageFormat.Jpeg;
                case ImageOutputFormat.Png:
                    return ImageFormat.Png;
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
        private string GetCacheFilePath(string originalPath, ImageSize outputSize, int quality, DateTime dateModified, ImageOutputFormat format, bool addPlayedIndicator, double? percentPlayed, int? unwatchedCount, string backgroundColor)
        {
            var filename = originalPath;

            filename += "width=" + outputSize.Width;

            filename += "height=" + outputSize.Height;

            filename += "quality=" + quality;

            filename += "datemodified=" + dateModified.Ticks;

            if (format != ImageOutputFormat.Original)
            {
                filename += "f=" + format;
            }

            var hasIndicator = false;

            if (addPlayedIndicator)
            {
                filename += "pl=true";
                hasIndicator = true;
            }

            if (percentPlayed.HasValue)
            {
                filename += "p=" + percentPlayed.Value;
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
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Guid GetImageCacheTag(IHasImages item, ImageType imageType, string imagePath)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            var dateModified = item.GetImageDateModified(imagePath);

            var supportedEnhancers = GetSupportedEnhancers(item, imageType);

            return GetImageCacheTag(item, imageType, imagePath, dateModified, supportedEnhancers.ToList());
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified of the original image file.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Guid GetImageCacheTag(IHasImages item, ImageType imageType, string originalImagePath, DateTime dateModified, List<IImageEnhancer> imageEnhancers)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (imageEnhancers == null)
            {
                throw new ArgumentNullException("imageEnhancers");
            }

            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            // Optimization
            if (imageEnhancers.Count == 0)
            {
                return (originalImagePath + dateModified.Ticks).GetMD5();
            }

            // Cache name is created with supported enhancers combined with the last config change so we pick up new config changes
            var cacheKeys = imageEnhancers.Select(i => i.GetConfigurationCacheKey(item, imageType)).ToList();
            cacheKeys.Add(originalImagePath + dateModified.Ticks);

            return string.Join("|", cacheKeys.ToArray()).GetMD5();
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

            var imagePath = item.GetImagePath(imageType, imageIndex);

            var dateModified = item.GetImageDateModified(imagePath);

            var result = await GetEnhancedImage(imagePath, dateModified, item, imageType, imageIndex, enhancers);

            return result.Item1;
        }

        private async Task<Tuple<string, DateTime>> GetEnhancedImage(string originalImagePath, DateTime dateModified, IHasImages item,
                                                    ImageType imageType, int imageIndex,
                                                    List<IImageEnhancer> enhancers)
        {
            try
            {
                // Enhance if we have enhancers
                var ehnancedImagePath = await GetEnhancedImageInternal(originalImagePath, dateModified, item, imageType, imageIndex, enhancers).ConfigureAwait(false);

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
        /// <param name="dateModified">The date modified of the original image file.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="supportedEnhancers">The supported enhancers.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">originalImagePath</exception>
        private async Task<string> GetEnhancedImageInternal(string originalImagePath, DateTime dateModified, IHasImages item, ImageType imageType, int imageIndex, List<IImageEnhancer> supportedEnhancers)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var cacheGuid = GetImageCacheTag(item, imageType, originalImagePath, dateModified, supportedEnhancers);

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

                        using (var originalImage = Image.FromStream(memoryStream, true, false))
                        {
                            //Pass the image through registered enhancers
                            using (var newImage = await ExecuteImageEnhancers(supportedEnhancers, originalImage, item, imageType, imageIndex).ConfigureAwait(false))
                            {
                                var parentDirectory = Path.GetDirectoryName(enhancedImagePath);

                                Directory.CreateDirectory(parentDirectory);

                                //And then save it in the cache
                                using (var outputStream = _fileSystem.GetFileStream(enhancedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read, false))
                                {
                                    newImage.Save(ImageFormat.Png, outputStream, 100);
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

        /// <summary>
        /// Executes the image enhancers.
        /// </summary>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <param name="originalImage">The original image.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{EnhancedImage}.</returns>
        private async Task<Image> ExecuteImageEnhancers(IEnumerable<IImageEnhancer> imageEnhancers, Image originalImage, IHasImages item, ImageType imageType, int imageIndex)
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
        private readonly ConcurrentDictionary<string, object> _locks = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private object GetObjectLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new object());
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
