using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
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
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Threading;
using MediaBrowser.Model.Extensions;

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
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IImageEnhancer[] ImageEnhancers { get; private set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationPaths _appPaths;
        private IImageEncoder _imageEncoder;
        private readonly Func<ILibraryManager> _libraryManager;
        private readonly Func<IMediaEncoder> _mediaEncoder;

        public ImageProcessor(ILogger logger,
            IServerApplicationPaths appPaths,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IImageEncoder imageEncoder,
            Func<ILibraryManager> libraryManager, ITimerFactory timerFactory, Func<IMediaEncoder> mediaEncoder)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            _imageEncoder = imageEncoder;
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;

            ImageEnhancers = new IImageEnhancer[] { };
            ImageHelper.ImageProcessor = this;
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
                return new string[]
                {
                    "tiff",
                    "tif",
                    "jpeg",
                    "jpg",
                    "png",
                    "aiff",
                    "cr2",
                    "crw",

                    // Remove until supported
                    //"nef", 
                    "orf",
                    "pef",
                    "arw",
                    "webp",
                    "gif",
                    "bmp",
                    "erf",
                    "raf",
                    "rw2",
                    "nrw",
                    "dng",
                    "ico",
                    "astc",
                    "ktx",
                    "pkm",
                    "wbmp"
                };
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

        private readonly string[] TransparentImageTypes = new string[] { ".png", ".webp", ".gif" };
        public bool SupportsTransparency(string path)
        {
            return TransparentImageTypes.Contains(Path.GetExtension(path) ?? string.Empty);
        }

        public async Task<Tuple<string, string, DateTime>> ProcessImage(ImageProcessingOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var originalImage = options.Image;
            var item = options.Item;

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
            var originalImageSize = originalImage.Width > 0 && originalImage.Height > 0 ? new ImageSize(originalImage.Width, originalImage.Height) : (ImageSize?)null;

            if (!_imageEncoder.SupportsImageEncoding)
            {
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            var supportedImageInfo = await GetSupportedImage(originalImagePath, dateModified).ConfigureAwait(false);
            originalImagePath = supportedImageInfo.Item1;
            dateModified = supportedImageInfo.Item2;
            var requiresTransparency = TransparentImageTypes.Contains(Path.GetExtension(originalImagePath) ?? string.Empty);

            if (options.Enhancers.Length > 0)
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

                }, requiresTransparency, item, options.ImageIndex, options.Enhancers, CancellationToken.None).ConfigureAwait(false);

                originalImagePath = tuple.Item1;
                dateModified = tuple.Item2;
                requiresTransparency = tuple.Item3;
                // TODO: Get this info
                originalImageSize = null;
            }

            var photo = item as Photo;
            var autoOrient = false;
            ImageOrientation? orientation = null;
            if (photo != null)
            {
                if (photo.Orientation.HasValue)
                {
                    if (photo.Orientation.Value != ImageOrientation.TopLeft)
                    {
                        autoOrient = true;
                        orientation = photo.Orientation;
                    }
                }
                else
                {
                    // Orientation unknown, so do it
                    autoOrient = true;
                    orientation = photo.Orientation;
                }
            }

            if (options.HasDefaultOptions(originalImagePath, originalImageSize) && (!autoOrient || !options.RequiresAutoOrientation))
            {
                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }

            //ImageSize? originalImageSize = GetSavedImageSize(originalImagePath, dateModified);
            //if (originalImageSize.HasValue && options.HasDefaultOptions(originalImagePath, originalImageSize.Value) && !autoOrient)
            //{
            //    // Just spit out the original file if all the options are default
            //    _logger.LogInformation("Returning original image {0}", originalImagePath);
            //    return new ValueTuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            //}

            var newSize = ImageHelper.GetNewImageSize(options, null);
            var quality = options.Quality;

            var outputFormat = GetOutputFormat(options.SupportedOutputFormats, requiresTransparency);
            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality, dateModified, outputFormat, options.AddPlayedIndicator, options.PercentPlayed, options.UnplayedCount, options.Blur, options.BackgroundColor, options.ForegroundLayer);

            CheckDisposed();

            var lockInfo = GetLock(cacheFilePath);

            await lockInfo.Lock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!_fileSystem.FileExists(cacheFilePath))
                {
                    if (options.CropWhiteSpace && !SupportsTransparency(originalImagePath))
                    {
                        options.CropWhiteSpace = false;
                    }

                    var resultPath = _imageEncoder.EncodeImage(originalImagePath, dateModified, cacheFilePath, autoOrient, orientation, quality, options, outputFormat);

                    if (string.Equals(resultPath, originalImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
                    }

                    return new Tuple<string, string, DateTime>(cacheFilePath, GetMimeType(outputFormat, cacheFilePath), _fileSystem.GetLastWriteTimeUtc(cacheFilePath));
                }

                return new Tuple<string, string, DateTime>(cacheFilePath, GetMimeType(outputFormat, cacheFilePath), _fileSystem.GetLastWriteTimeUtc(cacheFilePath));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Decoder failed to decode it
#if DEBUG
                _logger.LogError(ex, "Error encoding image");
#endif
                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }
            catch (Exception ex)
            {
                // If it fails for whatever reason, return the original image
                _logger.LogError(ex, "Error encoding image");

                // Just spit out the original file if all the options are default
                return new Tuple<string, string, DateTime>(originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
            }
            finally
            {
                ReleaseLock(cacheFilePath, lockInfo);
            }
        }

        private ImageFormat GetOutputFormat(ImageFormat[] clientSupportedFormats, bool requiresTransparency)
        {
            var serverFormats = GetSupportedImageOutputFormats();

            // Client doesn't care about format, so start with webp if supported
            if (serverFormats.Contains(ImageFormat.Webp) && clientSupportedFormats.Contains(ImageFormat.Webp))
            {
                return ImageFormat.Webp;
            }

            // If transparency is needed and webp isn't supported, than png is the only option
            if (requiresTransparency && clientSupportedFormats.Contains(ImageFormat.Png))
            {
                return ImageFormat.Png;
            }

            foreach (var format in clientSupportedFormats)
            {
                if (serverFormats.Contains(format))
                {
                    return format;
                }
            }

            // We should never actually get here
            return ImageFormat.Jpg;
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

        public ImageSize GetImageSize(BaseItem item, ItemImageInfo info)
        {
            return GetImageSize(item, info, false, true);
        }

        public ImageSize GetImageSize(BaseItem item, ItemImageInfo info, bool allowSlowMethods, bool updateItem)
        {
            var width = info.Width;
            var height = info.Height;

            if (height > 0 && width > 0)
            {
                return new ImageSize
                {
                    Width = width,
                    Height = height
                };
            }

            var path = info.Path;
            _logger.LogInformation("Getting image size for item {0} {1}", item.GetType().Name, path);

            var size = GetImageSize(path, allowSlowMethods);

            info.Height = Convert.ToInt32(size.Height);
            info.Width = Convert.ToInt32(size.Width);

            if (updateItem)
            {
                _libraryManager().UpdateImages(item);
            }

            return size;
        }

        public ImageSize GetImageSize(string path)
        {
            return GetImageSize(path, true);
        }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        private ImageSize GetImageSize(string path, bool allowSlowMethod)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            try
            {
                return ImageHeader.GetDimensions(path, _logger, _fileSystem);
            }
            catch
            {
                if (!allowSlowMethod)
                {
                    throw;
                }
            }

            return _imageEncoder.GetImageSize(path);
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageCacheTag(BaseItem item, ItemImageInfo image)
        {
            var supportedEnhancers = GetSupportedEnhancers(item, image.Type);

            return GetImageCacheTag(item, image, supportedEnhancers);
        }

        public string GetImageCacheTag(BaseItem item, ChapterInfo chapter)
        {
            try
            {
                return GetImageCacheTag(item, new ItemImageInfo
                {
                    Path = chapter.ImagePath,
                    Type = ImageType.Chapter,
                    DateModified = chapter.ImageDateModified
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImageCacheTag(BaseItem item, ItemImageInfo image, IImageEnhancer[] imageEnhancers)
        {
            var originalImagePath = image.Path;
            var dateModified = image.DateModified;
            var imageType = image.Type;

            // Optimization
            if (imageEnhancers.Length == 0)
            {
                return (originalImagePath + dateModified.Ticks).GetMD5().ToString("N");
            }

            // Cache name is created with supported enhancers combined with the last config change so we pick up new config changes
            var cacheKeys = imageEnhancers.Select(i => i.GetConfigurationCacheKey(item, imageType)).ToList();
            cacheKeys.Add(originalImagePath + dateModified.Ticks);

            return string.Join("|", cacheKeys.ToArray()).GetMD5().ToString("N");
        }

        private async Task<ValueTuple<string, DateTime>> GetSupportedImage(string originalImagePath, DateTime dateModified)
        {
            var inputFormat = (Path.GetExtension(originalImagePath) ?? string.Empty)
                .TrimStart('.')
                .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

            // These are just jpg files renamed as tbn
            if (string.Equals(inputFormat, "tbn", StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, DateTime>(originalImagePath, dateModified);
            }

            if (!_imageEncoder.SupportedInputFormats.Contains(inputFormat, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var filename = (originalImagePath + dateModified.Ticks.ToString(UsCulture)).GetMD5().ToString("N");

                    var cacheExtension = _mediaEncoder().SupportsEncoder("libwebp") ? ".webp" : ".png";
                    var outputPath = Path.Combine(_appPaths.ImageCachePath, "converted-images", filename + cacheExtension);

                    var file = _fileSystem.GetFileInfo(outputPath);
                    if (!file.Exists)
                    {
                        await _mediaEncoder().ConvertImage(originalImagePath, outputPath).ConfigureAwait(false);
                        dateModified = _fileSystem.GetLastWriteTimeUtc(outputPath);
                    }
                    else
                    {
                        dateModified = file.LastWriteTimeUtc;
                    }

                    originalImagePath = outputPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Image conversion failed for {originalImagePath}", originalImagePath);
                }
            }

            return new ValueTuple<string, DateTime>(originalImagePath, dateModified);
        }

        /// <summary>
        /// Gets the enhanced image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> GetEnhancedImage(BaseItem item, ImageType imageType, int imageIndex)
        {
            var enhancers = GetSupportedEnhancers(item, imageType);

            var imageInfo = item.GetImageInfo(imageType, imageIndex);

            var inputImageSupportsTransparency = SupportsTransparency(imageInfo.Path);

            var result = await GetEnhancedImage(imageInfo, inputImageSupportsTransparency, item, imageIndex, enhancers, CancellationToken.None);

            return result.Item1;
        }

        private async Task<ValueTuple<string, DateTime, bool>> GetEnhancedImage(ItemImageInfo image,
            bool inputImageSupportsTransparency,
            BaseItem item,
            int imageIndex,
            IImageEnhancer[] enhancers,
            CancellationToken cancellationToken)
        {
            var originalImagePath = image.Path;
            var dateModified = image.DateModified;
            var imageType = image.Type;

            try
            {
                var cacheGuid = GetImageCacheTag(item, image, enhancers);

                // Enhance if we have enhancers
                var enhancedImageInfo = await GetEnhancedImageInternal(originalImagePath, item, imageType, imageIndex, enhancers, cacheGuid, cancellationToken).ConfigureAwait(false);

                var enhancedImagePath = enhancedImageInfo.Item1;

                // If the path changed update dateModified
                if (!string.Equals(enhancedImagePath, originalImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    var treatmentRequiresTransparency = enhancedImageInfo.Item2;

                    return new ValueTuple<string, DateTime, bool>(enhancedImagePath, _fileSystem.GetLastWriteTimeUtc(enhancedImagePath), treatmentRequiresTransparency);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enhancing image");
            }

            return new ValueTuple<string, DateTime, bool>(originalImagePath, dateModified, inputImageSupportsTransparency);
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
        private async Task<ValueTuple<string, bool>> GetEnhancedImageInternal(string originalImagePath,
            BaseItem item,
            ImageType imageType,
            int imageIndex,
            IImageEnhancer[] supportedEnhancers,
            string cacheGuid,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var treatmentRequiresTransparency = false;
            foreach (var enhancer in supportedEnhancers)
            {
                if (!treatmentRequiresTransparency)
                {
                    treatmentRequiresTransparency = enhancer.GetEnhancedImageInfo(item, originalImagePath, imageType, imageIndex).RequiresTransparency;
                }
            }

            // All enhanced images are saved as png to allow transparency
            var cacheExtension = _imageEncoder.SupportedOutputFormats.Contains(ImageFormat.Webp) ?
                ".webp" :
                (treatmentRequiresTransparency ? ".png" : ".jpg");

            var enhancedImagePath = GetCachePath(EnhancedImageCachePath, cacheGuid + cacheExtension);

            var lockInfo = GetLock(enhancedImagePath);

            await lockInfo.Lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check again in case of contention
                if (_fileSystem.FileExists(enhancedImagePath))
                {
                    return new ValueTuple<string, bool>(enhancedImagePath, treatmentRequiresTransparency);
                }

                _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(enhancedImagePath));

                await ExecuteImageEnhancers(supportedEnhancers, originalImagePath, enhancedImagePath, item, imageType, imageIndex).ConfigureAwait(false);

                return new ValueTuple<string, bool>(enhancedImagePath, treatmentRequiresTransparency);
            }
            finally
            {
                ReleaseLock(enhancedImagePath, lockInfo);
            }
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
        private async Task ExecuteImageEnhancers(IEnumerable<IImageEnhancer> imageEnhancers, string inputPath, string outputPath, BaseItem item, ImageType imageType, int imageIndex)
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
            _logger.LogInformation("Creating image collage and saving to {0}", options.OutputPath);

            _imageEncoder.CreateImageCollage(options);

            _logger.LogInformation("Completed creation of image collage and saved to {0}", options.OutputPath);
        }

        public IImageEnhancer[] GetSupportedEnhancers(BaseItem item, ImageType imageType)
        {
            List<IImageEnhancer> list = null;

            foreach (var i in ImageEnhancers)
            {
                try
                {
                    if (i.Supports(item, imageType))
                    {
                        if (list == null)
                        {
                            list = new List<IImageEnhancer>();
                        }
                        list.Add(i);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in image enhancer: {0}", i.GetType().Name);
                }
            }

            return list == null ? Array.Empty<IImageEnhancer>() : list.ToArray();
        }

        private Dictionary<string, LockInfo> _locks = new Dictionary<string, LockInfo>();
        private class LockInfo
        {
            public SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
            public int Count = 1;
        }
        private LockInfo GetLock(string key)
        {
            lock (_locks)
            {
                LockInfo info;
                if (_locks.TryGetValue(key, out info))
                {
                    info.Count++;
                }
                else
                {
                    info = new LockInfo();
                    _locks[key] = info;
                }
                return info;
            }
        }

        private void ReleaseLock(string key, LockInfo info)
        {
            info.Lock.Release();

            lock (_locks)
            {
                info.Count--;
                if (info.Count <= 0)
                {
                    _locks.Remove(key);
                    info.Lock.Dispose();
                }
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;

            var disposable = _imageEncoder as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
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
