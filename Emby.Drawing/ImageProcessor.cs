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
using MediaBrowser.Model.IO;
using Emby.Drawing.Common;

using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Threading;
using TagLib;

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
        private IImageEncoder _imageEncoder;
        private readonly Func<ILibraryManager> _libraryManager;

        public ImageProcessor(ILogger logger,
            IServerApplicationPaths appPaths,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IImageEncoder imageEncoder,
            Func<ILibraryManager> libraryManager, ITimerFactory timerFactory)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _imageEncoder = imageEncoder;
            _libraryManager = libraryManager;
            _appPaths = appPaths;

            ImageEnhancers = new List<IImageEnhancer>();
            _saveImageSizeTimer = timerFactory.Create(SaveImageSizeCallback, null, Timeout.Infinite, Timeout.Infinite);
            ImageHelper.ImageProcessor = this;

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
            catch (IOException)
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

        public IImageEncoder ImageEncoder
        {
            get { return _imageEncoder; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _imageEncoder = value;
            }
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

        public void AddParts(IEnumerable<IImageEnhancer> enhancers)
        {
            ImageEnhancers = enhancers.ToArray();
        }

        public async Task ProcessImage(ImageProcessingOptions options, Stream toStream)
        {
            var file = await ProcessImage(options).ConfigureAwait(false);

            using (var fileStream = _fileSystem.GetFileStream(file.Item1, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read, true))
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
            IHasImages item = options.Item;

            if (!originalImage.IsLocalFile)
            {
                if (item == null)
                {
                    item = _libraryManager().GetItemById(options.ItemId);
                }
                originalImage = await _libraryManager().ConvertImageToLocal(item, originalImage, options.ImageIndex).ConfigureAwait(false);
            }

            var originalImagePath = originalImage.Path;
            var dateModified = originalImage.DateModified;

            if (!_imageEncoder.SupportsImageEncoding)
            {
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            if (options.Enhancers.Count > 0)
            {
                if (item == null)
                {
                    item = _libraryManager().GetItemById(options.ItemId);
                }

                var tuple = await GetEnhancedImage(new ItemImageInfo
                {
                    DateModified = dateModified,
                    Type = originalImage.Type,
                    Path = originalImagePath

                }, item, options.ImageIndex, options.Enhancers).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
            }

            if (options.HasDefaultOptions(originalImagePath))
            {
                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            ImageSize? originalImageSize = GetSavedImageSize(originalImagePath, dateModified);
            if (originalImageSize.HasValue && options.HasDefaultOptions(originalImagePath, originalImageSize.Value))
            {
                // Just spit out the original file if all the options are default
                _logger.Info("Returning original image {0}", originalImagePath);
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            var newSize = ImageHelper.GetNewImageSize(options, originalImageSize);
            var quality = options.Quality;

            var outputFormat = GetOutputFormat(options.SupportedOutputFormats[0]);
            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality, dateModified, outputFormat, options.AddPlayedIndicator, options.PercentPlayed, options.UnplayedCount, options.Blur, options.BackgroundColor, options.ForegroundLayer);

            try
            {
                CheckDisposed();

                if (!_fileSystem.FileExists(cacheFilePath))
                {
                    _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(cacheFilePath));
                    var tmpPath = Path.ChangeExtension(Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString("N")), Path.GetExtension(cacheFilePath));
                    _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(tmpPath));

                    if (item == null && string.Equals(options.ItemType, typeof(Photo).Name, StringComparison.OrdinalIgnoreCase))
                    {
                        item = _libraryManager().GetItemById(options.ItemId);
                    }

                    var resultPath =_imageEncoder.EncodeImage(originalImagePath, dateModified, tmpPath, AutoOrient(item), quality, options, outputFormat);

                    if (string.Equals(resultPath, originalImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
                    }

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
        }

        private void CopyFile(string src, string destination)
        {
            try
            {
                _fileSystem.CopyFile(src, destination, true);
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

        private ImageFormat GetOutputFormat(ImageFormat requestedFormat)
        {
            if (requestedFormat == ImageFormat.Webp && !_imageEncoder.SupportedOutputFormats.Contains(ImageFormat.Webp))
            {
                return ImageFormat.Png;
            }

            return requestedFormat;
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
        private string GetCacheFilePath(string originalPath, ImageSize outputSize, int quality, DateTime dateModified, ImageFormat format, bool addPlayedIndicator, double percentPlayed, int? unwatchedCount, int? blur, string backgroundColor, string foregroundLayer)
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

            if (blur.HasValue)
            {
                filename += "blur=" + blur.Value;
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

            ImageSize size;

            var cacheHash = GetImageSizeKey(path, imageDateModified);

            if (!_cachedImagedSizes.TryGetValue(cacheHash, out size))
            {
                size = GetImageSizeInternal(path, allowSlowMethod);

                SaveImageSize(size, cacheHash, false);
            }

            return size;
        }

        public void SaveImageSize(string path, DateTime imageDateModified, ImageSize size)
        {
            var cacheHash = GetImageSizeKey(path, imageDateModified);
            SaveImageSize(size, cacheHash, true);
        }

        private void SaveImageSize(ImageSize size, Guid cacheHash, bool checkExists)
        {
            if (size.Width <= 0 || size.Height <= 0)
            {
                return;
            }

            if (checkExists && _cachedImagedSizes.ContainsKey(cacheHash))
            {
                return;
            }

            if (checkExists)
            {
                if (_cachedImagedSizes.TryAdd(cacheHash, size))
                {
                    StartSaveImageSizeTimer();
                }
            }
            else
            {
                StartSaveImageSizeTimer();
                _cachedImagedSizes.AddOrUpdate(cacheHash, size, (keyName, oldValue) => size);
            }
        }

        private Guid GetImageSizeKey(string path, DateTime imageDateModified)
        {
            var name = path + "datemodified=" + imageDateModified.Ticks;
            return name.GetMD5();
        }

        public ImageSize? GetSavedImageSize(string path, DateTime imageDateModified)
        {
            ImageSize size;

            var cacheHash = GetImageSizeKey(path, imageDateModified);

            if (_cachedImagedSizes.TryGetValue(cacheHash, out size))
            {
                return size;
            }

            return null;
        }

        /// <summary>
        /// Gets the image size internal.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="allowSlowMethod">if set to <c>true</c> [allow slow method].</param>
        /// <returns>ImageSize.</returns>
        private ImageSize GetImageSizeInternal(string path, bool allowSlowMethod)
        {
            //try
            //{
            //    using (var fileStream = _fileSystem.OpenRead(path))
            //    {
            //        using (var file = TagLib.File.Create(new StreamFileAbstraction(Path.GetFileName(path), fileStream, null)))
            //        {
            //            var image = file as TagLib.Image.File;

            //            if (image != null)
            //            {
            //                var properties = image.Properties;

            //                return new ImageSize
            //                {
            //                    Height = properties.PhotoHeight,
            //                    Width = properties.PhotoWidth
            //                };
            //            }
            //        }
            //    }
            //}
            //catch
            //{
            //}

            try
            {
                return ImageHeader.GetDimensions(path, _logger, _fileSystem);
            }
            catch
            {
                if (allowSlowMethod)
                {
                    return _imageEncoder.GetImageSize(path);
                }

                throw;
            }
        }

        private readonly ITimer _saveImageSizeTimer;
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
                    _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(path));
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
                if (!string.Equals(ehnancedImagePath, originalImagePath, StringComparison.OrdinalIgnoreCase))
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

            // Check again in case of contention
            if (_fileSystem.FileExists(enhancedImagePath))
            {
                return enhancedImagePath;
            }

            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(enhancedImagePath));

            var tmpPath = Path.Combine(_appPaths.TempDirectory, Path.ChangeExtension(Guid.NewGuid().ToString(), Path.GetExtension(enhancedImagePath)));
            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(tmpPath));

            await ExecuteImageEnhancers(supportedEnhancers, originalImagePath, tmpPath, item, imageType, imageIndex).ConfigureAwait(false);

            try
            {
                _fileSystem.CopyFile(tmpPath, enhancedImagePath, true);
            }
            catch
            {

            }

            return tmpPath;
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
                await enhancer.EnhanceImageAsync(item, inputPath, outputPath, imageType, imageIndex).ConfigureAwait(false);

                // Feed the output into the next enhancer as input
                inputPath = outputPath;
            }
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

        public void CreateImageCollage(ImageCollageOptions options)
        {
            _logger.Info("Creating image collage and saving to {0}", options.OutputPath);

            _imageEncoder.CreateImageCollage(options);

            _logger.Info("Completed creation of image collage and saved to {0}", options.OutputPath);
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