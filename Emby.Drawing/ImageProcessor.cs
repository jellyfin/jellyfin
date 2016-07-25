using MediaBrowser.Common.Extensions;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using Emby.Drawing.Common;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Net;

namespace Emby.Drawing
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
        private readonly IImageEncoder _imageEncoder;
        private readonly SemaphoreSlim _imageProcessingSemaphore;
        private readonly Func<ILibraryManager> _libraryManager;

        public ImageProcessor(ILogger logger,
            IServerApplicationPaths appPaths,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IImageEncoder imageEncoder,
            int maxConcurrentImageProcesses, Func<ILibraryManager> libraryManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _imageEncoder = imageEncoder;
            _libraryManager = libraryManager;
            _appPaths = appPaths;

            ImageEnhancers = new List<IImageEnhancer>();
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
            catch (DirectoryNotFoundException)
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
            _logger.Info("ImageProcessor started with {0} max concurrent image processes", maxConcurrentImageProcesses);
            _imageProcessingSemaphore = new SemaphoreSlim(maxConcurrentImageProcesses, maxConcurrentImageProcesses);
        }

        public string[] SupportedInputFormats
        {
            get
            {
                return _imageEncoder.SupportedInputFormats;
            }
        }


        public bool SupportsImageCollageCreation
        {
            get
            {
                return _imageEncoder.SupportsImageCollageCreation;
            }
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

            using (var fileStream = _fileSystem.GetFileStream(file.Item1, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
            }
        }

        public ImageFormat[] GetSupportedImageOutputFormats()
        {
            return _imageEncoder.SupportedOutputFormats;
        }

        public async Task<Tuple<string, string, DateTime>> ProcessImage(ImageProcessingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var originalImage = options.Image;

            if (!originalImage.IsLocalFile)
            {
                originalImage = await _libraryManager().ConvertImageToLocal(options.Item, originalImage, options.ImageIndex).ConfigureAwait(false);
            }

            var originalImagePath = originalImage.Path;
            var dateModified = originalImage.DateModified;

            if (!_imageEncoder.SupportsImageEncoding)
            {
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            if (options.CropWhiteSpace && _imageEncoder.SupportsImageEncoding)
            {
                var tuple = await GetWhitespaceCroppedImage(originalImagePath, dateModified).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            if (options.Enhancers.Count > 0)
            {
                var tuple = await GetEnhancedImage(new ItemImageInfo
                {
                    DateModified = dateModified,
                    Type = originalImage.Type,
                    Path = originalImagePath

                }, options.Item, options.ImageIndex, options.Enhancers).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            if (options.HasDefaultOptions(originalImagePath))
            {
                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            ImageSize? originalImageSize;
            try
            {
                originalImageSize = GetImageSize(originalImagePath, dateModified, true);
                if (options.HasDefaultOptions(originalImagePath, originalImageSize.Value))
                {
                    // Just spit out the original file if all the options are default
                    return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
                }
            }
            catch
            {
                originalImageSize = null;
            }

            var newSize = GetNewImageSize(options, originalImageSize);
            var quality = options.Quality;

            var outputFormat = GetOutputFormat(options.SupportedOutputFormats[0]);
            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality, dateModified, outputFormat, options.AddPlayedIndicator, options.PercentPlayed, options.UnplayedCount, options.BackgroundColor, options.ForegroundLayer);

            var imageProcessingLockTaken = false;

            try
            {
                CheckDisposed();

                if (!_fileSystem.FileExists(cacheFilePath))
                {
                    var newWidth = Convert.ToInt32(newSize.Width);
                    var newHeight = Convert.ToInt32(newSize.Height);

                    _fileSystem.CreateDirectory(Path.GetDirectoryName(cacheFilePath));
                    var tmpPath = Path.ChangeExtension(Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString("N")), Path.GetExtension(cacheFilePath));
                    _fileSystem.CreateDirectory(Path.GetDirectoryName(tmpPath));

                    await _imageProcessingSemaphore.WaitAsync().ConfigureAwait(false);

                    imageProcessingLockTaken = true;

                    _imageEncoder.EncodeImage(originalImagePath, tmpPath, AutoOrient(options.Item), newWidth, newHeight, quality, options, outputFormat);
                    CopyFile(tmpPath, cacheFilePath);

                    return new Tuple<string, string, DateTime>(tmpPath, GetMimeType(outputFormat, cacheFilePath), _fileSystem.GetLastWriteTimeUtc(tmpPath));
                }

                return new Tuple<string, string, DateTime>(cacheFilePath, GetMimeType(outputFormat, cacheFilePath), _fileSystem.GetLastWriteTimeUtc(cacheFilePath));
            }
            catch (Exception ex)
            {
                // If it fails for whatever reason, return the original image
                _logger.ErrorException("Error encoding image", ex);

                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }
            finally
            {
                if (imageProcessingLockTaken)
                {
                    _imageProcessingSemaphore.Release();
                }
            }
        }

        private void CopyFile(string src, string destination)
        {
            try
            {
                File.Copy(src, destination, true);
            }
            catch
            {

            }
        }

        private bool AutoOrient(IHasImages item)
        {
            var photo = item as Photo;
            if (photo != null && photo.Orientation.HasValue)
            {
                return true;
            }

            return false;
        }

        //private static  int[][] OPERATIONS = new int[][] {
        // TopLeft
        //new int[] {  0, NONE},
        // TopRight
        //new int[] {  0, HORIZONTAL},
        //new int[] {180, NONE},
        // LeftTop
        //new int[] {  0, VERTICAL},
        //new int[] { 90, HORIZONTAL},
        // RightTop
        //new int[] { 90, NONE},
        //new int[] {-90, HORIZONTAL},
        //new int[] {-90, NONE},
        //};

        private string GetMimeType(ImageFormat format, string path)
        {
            if (format == ImageFormat.Bmp)
            {
                return MimeTypes.GetMimeType("i.bmp");
            }
            if (format == ImageFormat.Gif)
            {
                return MimeTypes.GetMimeType("i.gif");
            }
            if (format == ImageFormat.Jpg)
            {
                return MimeTypes.GetMimeType("i.jpg");
            }
            if (format == ImageFormat.Png)
            {
                return MimeTypes.GetMimeType("i.png");
            }
            if (format == ImageFormat.Webp)
            {
                return MimeTypes.GetMimeType("i.webp");
            }

            return MimeTypes.GetMimeType(path);
        }

        private ImageSize GetNewImageSize(ImageProcessingOptions options, ImageSize? originalImageSize)
        {
            if (originalImageSize.HasValue)
            {
                // Determine the output size based on incoming parameters
                var newSize = DrawingUtils.Resize(originalImageSize.Value, options.Width, options.Height, options.MaxWidth, options.MaxHeight);

                return newSize;
            }
            return GetSizeEstimate(options);
        }

        private ImageSize GetSizeEstimate(ImageProcessingOptions options)
        {
            if (options.Width.HasValue && options.Height.HasValue)
            {
                return new ImageSize(options.Width.Value, options.Height.Value);
            }

            var aspect = GetEstimatedAspectRatio(options.Image.Type);

            var width = options.Width ?? options.MaxWidth;

            if (width.HasValue)
            {
                var heightValue = aspect / width.Value;
                return new ImageSize(width.Value, Convert.ToInt32(heightValue));
            }

            var height = options.Height ?? options.MaxHeight ?? 200;
            var widthValue = aspect * height;
            return new ImageSize(Convert.ToInt32(widthValue), height);
        }

        private double GetEstimatedAspectRatio(ImageType type)
        {
            switch (type)
            {
                case ImageType.Art:
                case ImageType.Backdrop:
                case ImageType.Chapter:
                case ImageType.Screenshot:
                case ImageType.Thumb:
                    return 1.78;
                case ImageType.Banner:
                    return 5.4;
                case ImageType.Box:
                case ImageType.BoxRear:
                case ImageType.Disc:
                case ImageType.Menu:
                    return 1;
                case ImageType.Logo:
                    return 2.58;
                case ImageType.Primary:
                    return .667;
                default:
                    return 1;
            }
        }

        private ImageFormat GetOutputFormat(ImageFormat requestedFormat)
        {
            if (requestedFormat == ImageFormat.Webp && !_imageEncoder.SupportedOutputFormats.Contains(ImageFormat.Webp))
            {
                return ImageFormat.Png;
            }

            return requestedFormat;
        }

        /// <summary>
        /// Crops whitespace from an image, caches the result, and returns the cached path
        /// </summary>
        private async Task<Tuple<string, DateTime>> GetWhitespaceCroppedImage(string originalImagePath, DateTime dateModified)
        {
            var name = originalImagePath;
            name += "datemodified=" + dateModified.Ticks;

            var croppedImagePath = GetCachePath(CroppedWhitespaceImageCachePath, name, Path.GetExtension(originalImagePath));

            // Check again in case of contention
            if (_fileSystem.FileExists(croppedImagePath))
            {
                return GetResult(croppedImagePath);
            }

            var imageProcessingLockTaken = false;

            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(croppedImagePath));
                var tmpPath = Path.ChangeExtension(Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString("N")), Path.GetExtension(croppedImagePath));
                _fileSystem.CreateDirectory(Path.GetDirectoryName(tmpPath));

                await _imageProcessingSemaphore.WaitAsync().ConfigureAwait(false);
                imageProcessingLockTaken = true;

                _imageEncoder.CropWhiteSpace(originalImagePath, tmpPath);
                CopyFile(tmpPath, croppedImagePath);
                return GetResult(tmpPath);
            }
            catch (NotImplementedException)
            {
                // No need to spam the log with an error message
                return new Tuple<string, DateTime>(originalImagePath, dateModified);
            }
            catch (Exception ex)
            {
                // We have to have a catch-all here because some of the .net image methods throw a plain old Exception
                _logger.ErrorException("Error cropping image {0}", ex, originalImagePath);

                return new Tuple<string, DateTime>(originalImagePath, dateModified);
            }
            finally
            {
                if (imageProcessingLockTaken)
                {
                    _imageProcessingSemaphore.Release();
                }
            }
        }

        private Tuple<string, DateTime> GetResult(string path)
        {
            return new Tuple<string, DateTime>(path, _fileSystem.GetLastWriteTimeUtc(path));
        }

        /// <summary>
        /// Increment this when there's a change requiring caches to be invalidated
        /// </summary>
        private const string Version = "3";

        /// <summary>
        /// Gets the cache file path based on a set of parameters
        /// </summary>
        private string GetCacheFilePath(string originalPath, ImageSize outputSize, int quality, DateTime dateModified, ImageFormat format, bool addPlayedIndicator, double percentPlayed, int? unwatchedCount, string backgroundColor, string foregroundLayer)
        {
            var filename = originalPath;

            filename += "width=" + outputSize.Width;

            filename += "height=" + outputSize.Height;

            filename += "quality=" + quality;

            filename += "datemodified=" + dateModified.Ticks;

            filename += "f=" + format;

            if (addPlayedIndicator)
            {
                filename += "pl=true";
            }

            if (percentPlayed > 0)
            {
                filename += "p=" + percentPlayed;
            }

            if (unwatchedCount.HasValue)
            {
                filename += "p=" + unwatchedCount.Value;
            }

            if (!string.IsNullOrEmpty(backgroundColor))
            {
                filename += "b=" + backgroundColor;
            }

            if (!string.IsNullOrEmpty(foregroundLayer))
            {
                filename += "fl=" + foregroundLayer;
            }

            filename += "v=" + Version;

            return GetCachePath(ResizedImageCachePath, filename, "." + format.ToString().ToLower());
        }

        public ImageSize GetImageSize(ItemImageInfo info)
        {
            return GetImageSize(info.Path, info.DateModified, false);
        }

        public ImageSize GetImageSize(string path)
        {
            return GetImageSize(path, _fileSystem.GetLastWriteTimeUtc(path), false);
        }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="imageDateModified">The image date modified.</param>
        /// <param name="allowSlowMethod">if set to <c>true</c> [allow slow method].</param>
        /// <returns>ImageSize.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        private ImageSize GetImageSize(string path, DateTime imageDateModified, bool allowSlowMethod)
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
                size = GetImageSizeInternal(path, allowSlowMethod);

                if (size.Width > 0 && size.Height > 0)
                {
                    StartSaveImageSizeTimer();
                    _cachedImagedSizes.AddOrUpdate(cacheHash, size, (keyName, oldValue) => size);
                }
            }

            return size;
        }

        /// <summary>
        /// Gets the image size internal.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="allowSlowMethod">if set to <c>true</c> [allow slow method].</param>
        /// <returns>ImageSize.</returns>
        private ImageSize GetImageSizeInternal(string path, bool allowSlowMethod)
        {
            try
            {
                using (var file = TagLib.File.Create(path))
                {
                    var image = file as TagLib.Image.File;

                    var properties = image.Properties;

                    return new ImageSize
                    {
                        Height = properties.PhotoHeight,
                        Width = properties.PhotoWidth
                    };
                }
            }
            catch
            {
            }

            return ImageHeader.GetDimensions(path, _logger, _fileSystem);
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
                    _fileSystem.CreateDirectory(Path.GetDirectoryName(path));
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
                    return GetResult(ehnancedImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error enhancing image", ex);
            }

            return new Tuple<string, DateTime>(originalImagePath, dateModified);
        }

        /// <summary>
        /// Gets the enhanced image internal.
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="supportedEnhancers">The supported enhancers.</param>
        /// <param name="cacheGuid">The cache unique identifier.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="ArgumentNullException">
        /// originalImagePath
        /// or
        /// item
        /// </exception>
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
            if (_fileSystem.FileExists(enhancedImagePath))
            {
                semaphore.Release();
                return enhancedImagePath;
            }

            var imageProcessingLockTaken = false;

            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(enhancedImagePath));

                await _imageProcessingSemaphore.WaitAsync().ConfigureAwait(false);

                imageProcessingLockTaken = true;

                await ExecuteImageEnhancers(supportedEnhancers, originalImagePath, enhancedImagePath, item, imageType, imageIndex).ConfigureAwait(false);
            }
            finally
            {
                if (imageProcessingLockTaken)
                {
                    _imageProcessingSemaphore.Release();
                }

                semaphore.Release();
            }

            return enhancedImagePath;
        }

        /// <summary>
        /// Executes the image enhancers.
        /// </summary>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <param name="inputPath">The input path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{EnhancedImage}.</returns>
        private async Task ExecuteImageEnhancers(IEnumerable<IImageEnhancer> imageEnhancers, string inputPath, string outputPath, IHasImages item, ImageType imageType, int imageIndex)
        {
            // Run the enhancers sequentially in order of priority
            foreach (var enhancer in imageEnhancers)
            {
                var typeName = enhancer.GetType().Name;

                try
                {
                    await enhancer.EnhanceImageAsync(item, inputPath, outputPath, imageType, imageIndex).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("{0} failed enhancing {1}", ex, typeName, item.Name);

                    throw;
                }

                // Feed the output into the next enhancer as input
                inputPath = outputPath;
            }
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

        public async Task CreateImageCollage(ImageCollageOptions options)
        {
            await _imageProcessingSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                _logger.Info("Creating image collage and saving to {0}", options.OutputPath);

                _imageEncoder.CreateImageCollage(options);

                _logger.Info("Completed creation of image collage and saved to {0}", options.OutputPath);
            }
            finally
            {
                _imageProcessingSemaphore.Release();
            }
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

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            _imageEncoder.Dispose();
            _saveImageSizeTimer.Dispose();
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