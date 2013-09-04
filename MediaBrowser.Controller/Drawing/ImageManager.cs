using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
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

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Class ImageManager
    /// </summary>
    public class ImageManager
    {
        /// <summary>
        /// Gets the list of currently registered image processors
        /// Image processors are specialized metadata providers that run after the normal ones
        /// </summary>
        /// <value>The image enhancers.</value>
        public IEnumerable<IImageEnhancer> ImageEnhancers { get; set; }

        /// <summary>
        /// Gets the image size cache.
        /// </summary>
        /// <value>The image size cache.</value>
        private FileSystemRepository ImageSizeCache { get; set; }

        /// <summary>
        /// Gets or sets the resized image cache.
        /// </summary>
        /// <value>The resized image cache.</value>
        private FileSystemRepository ResizedImageCache { get; set; }
        /// <summary>
        /// Gets the cropped image cache.
        /// </summary>
        /// <value>The cropped image cache.</value>
        private FileSystemRepository CroppedImageCache { get; set; }

        /// <summary>
        /// Gets the cropped image cache.
        /// </summary>
        /// <value>The cropped image cache.</value>
        private FileSystemRepository EnhancedImageCache { get; set; }

        /// <summary>
        /// The cached imaged sizes
        /// </summary>
        private readonly ConcurrentDictionary<string, ImageSize> _cachedImagedSizes = new ConcurrentDictionary<string, ImageSize>();

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageManager" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="itemRepo">The item repo.</param>
        public ImageManager(ILogger logger, IServerApplicationPaths appPaths, IItemRepository itemRepo)
        {
            _logger = logger;
            _itemRepo = itemRepo;

            ImageSizeCache = new FileSystemRepository(Path.Combine(appPaths.ImageCachePath, "image-sizes"));
            ResizedImageCache = new FileSystemRepository(Path.Combine(appPaths.ImageCachePath, "resized-images"));
            CroppedImageCache = new FileSystemRepository(Path.Combine(appPaths.ImageCachePath, "cropped-images"));
            EnhancedImageCache = new FileSystemRepository(Path.Combine(appPaths.ImageCachePath, "enhanced-images"));
        }

        /// <summary>
        /// Processes an image by resizing to target dimensions
        /// </summary>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="cropWhitespace">if set to <c>true</c> [crop whitespace].</param>
        /// <param name="dateModified">The last date modified of the original image file</param>
        /// <param name="toStream">The stream to save the new image to</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        /// <param name="enhancers">The enhancers.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">entity</exception>
        public async Task ProcessImage(BaseItem entity, ImageType imageType, int imageIndex, string originalImagePath, bool cropWhitespace, DateTime dateModified, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality, List<IImageEnhancer> enhancers)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            if (toStream == null)
            {
                throw new ArgumentNullException("toStream");
            }

            if (cropWhitespace)
            {
                originalImagePath = await GetCroppedImage(originalImagePath, dateModified).ConfigureAwait(false);
            }

            // No enhancement - don't cache
            if (enhancers.Count > 0)
            {
                try
                {
                    // Enhance if we have enhancers
                    var ehnancedImagePath = await GetEnhancedImage(originalImagePath, dateModified, entity, imageType, imageIndex, enhancers).ConfigureAwait(false);

                    // If the path changed update dateModified
                    if (!ehnancedImagePath.Equals(originalImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        dateModified = File.GetLastWriteTimeUtc(ehnancedImagePath);
                        originalImagePath = ehnancedImagePath;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error enhancing image", ex);
                }
            }

            var originalImageSize = await GetImageSize(originalImagePath, dateModified).ConfigureAwait(false);

            // Determine the output size based on incoming parameters
            var newSize = DrawingUtils.Resize(originalImageSize, width, height, maxWidth, maxHeight);

            if (!quality.HasValue)
            {
                quality = 90;
            }

            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality.Value, dateModified);

            try
            {
                using (var fileStream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                    return;
                }
            }
            catch (IOException)
            {
                // Cache file doesn't exist or is currently being written ro
            }

            var semaphore = GetLock(cacheFilePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of lock contention
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    using (var fileStream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                    {
                        await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                        return;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }

            try
            {
                using (var fileStream = new FileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
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
                            using (var thumbnail = !ImageExtensions.IsPixelFormatSupportedByGraphicsObject(originalImage.PixelFormat) ? new Bitmap(originalImage, newWidth, newHeight) : new Bitmap(newWidth, newHeight, originalImage.PixelFormat))
                            {
                                // Preserve the original resolution
                                thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

                                using (var thumbnailGraph = Graphics.FromImage(thumbnail))
                                {
                                    thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                                    thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                                    thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    thumbnailGraph.CompositingMode = CompositingMode.SourceOver;

                                    thumbnailGraph.DrawImage(originalImage, 0, 0, newWidth, newHeight);

                                    var outputFormat = originalImage.RawFormat;

                                    using (var outputMemoryStream = new MemoryStream())
                                    {
                                        // Save to the memory stream
                                        thumbnail.Save(outputFormat, outputMemoryStream, quality.Value);

                                        var bytes = outputMemoryStream.ToArray();

                                        var outputTask = toStream.WriteAsync(bytes, 0, bytes.Length);

                                        // kick off a task to cache the result
                                        var cacheTask = CacheResizedImage(cacheFilePath, bytes);

                                        await Task.WhenAll(outputTask, cacheTask).ConfigureAwait(false);
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
        }

        /// <summary>
        /// Caches the resized image.
        /// </summary>
        /// <param name="cacheFilePath">The cache file path.</param>
        /// <param name="bytes">The bytes.</param>
        private async Task CacheResizedImage(string cacheFilePath, byte[] bytes)
        {
            var parentPath = Path.GetDirectoryName(cacheFilePath);

            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            // Save to the cache location
            using (var cacheFileStream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
            {
                // Save to the filestream
                await cacheFileStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the cache file path based on a set of parameters
        /// </summary>
        /// <param name="originalPath">The path to the original image file</param>
        /// <param name="outputSize">The size to output the image in</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        /// <param name="dateModified">The last modified date of the image</param>
        /// <returns>System.String.</returns>
        private string GetCacheFilePath(string originalPath, ImageSize outputSize, int quality, DateTime dateModified)
        {
            var filename = originalPath;

            filename += "width=" + outputSize.Width;

            filename += "height=" + outputSize.Height;

            filename += "quality=" + quality;

            filename += "datemodified=" + dateModified.Ticks;

            return ResizedImageCache.GetResourcePath(filename, Path.GetExtension(originalPath));
        }


        /// <summary>
        /// Gets image dimensions
        /// </summary>
        /// <param name="imagePath">The image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns>Task{ImageSize}.</returns>
        /// <exception cref="System.ArgumentNullException">imagePath</exception>
        public async Task<ImageSize> GetImageSize(string imagePath, DateTime dateModified)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            var name = imagePath + "datemodified=" + dateModified.Ticks;

            ImageSize size;

            if (!_cachedImagedSizes.TryGetValue(name, out size))
            {
                size = await GetImageSize(name, imagePath).ConfigureAwait(false);

                _cachedImagedSizes.AddOrUpdate(name, size, (keyName, oldValue) => size);
            }

            return size;
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>ImageSize.</returns>
        private async Task<ImageSize> GetImageSize(string keyName, string imagePath)
        {
            // Now check the file system cache
            var fullCachePath = ImageSizeCache.GetResourcePath(keyName, ".txt");

            try
            {
                var result = File.ReadAllText(fullCachePath).Split('|').Select(i => double.Parse(i, UsCulture)).ToArray();

                return new ImageSize { Width = result[0], Height = result[1] };
            }
            catch (IOException)
            {
                // Cache file doesn't exist or is currently being written to
            }

            var semaphore = GetLock(fullCachePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var result = File.ReadAllText(fullCachePath).Split('|').Select(i => double.Parse(i, UsCulture)).ToArray();

                return new ImageSize { Width = result[0], Height = result[1] };
            }
            catch (FileNotFoundException)
            {
                // Cache file doesn't exist no biggie
            }
            catch (DirectoryNotFoundException)
            {
                // Cache file doesn't exist no biggie
            }
            catch
            {
                semaphore.Release();

                throw;
            }

            try
            {
                var size = await ImageHeader.GetDimensions(imagePath, _logger).ConfigureAwait(false);

                var parentPath = Path.GetDirectoryName(fullCachePath);

                if (!Directory.Exists(parentPath))
                {
                    Directory.CreateDirectory(parentPath);
                }

                // Update the file system cache
                File.WriteAllText(fullCachePath, size.Width.ToString(UsCulture) + @"|" + size.Height.ToString(UsCulture));

                return new ImageSize { Width = size.Width, Height = size.Height };
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public string GetImagePath(BaseItem item, ImageType imageType, int imageIndex)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (imageType == ImageType.Backdrop)
            {
                if (item.BackdropImagePaths == null)
                {
                    throw new InvalidOperationException(string.Format("Item {0} does not have any Backdrops.", item.Name));
                }

                return item.BackdropImagePaths[imageIndex];
            }

            if (imageType == ImageType.Screenshot)
            {
                if (item.ScreenshotImagePaths == null)
                {
                    throw new InvalidOperationException(string.Format("Item {0} does not have any Screenshots.", item.Name));
                }

                return item.ScreenshotImagePaths[imageIndex];
            }

            if (imageType == ImageType.Chapter)
            {
                return _itemRepo.GetChapter(item.Id, imageIndex).ImagePath;
            }

            return item.GetImage(imageType);
        }

        /// <summary>
        /// Gets the image date modified.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>DateTime.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public DateTime GetImageDateModified(BaseItem item, ImageType imageType, int imageIndex)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var imagePath = GetImagePath(item, imageType, imageIndex);

            return GetImageDateModified(item, imagePath);
        }

        /// <summary>
        /// Gets the image date modified.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>DateTime.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public DateTime GetImageDateModified(BaseItem item, string imagePath)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            var metaFileEntry = item.ResolveArgs.GetMetaFileByPath(imagePath);

            // If we didn't the metafile entry, check the Season
            if (metaFileEntry == null)
            {
                var episode = item as Episode;

                if (episode != null && episode.Season != null)
                {
                    episode.Season.ResolveArgs.GetMetaFileByPath(imagePath);
                }
            }

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            return metaFileEntry == null ? File.GetLastWriteTimeUtc(imagePath) : metaFileEntry.LastWriteTimeUtc;
        }

        /// <summary>
        /// Crops whitespace from an image, caches the result, and returns the cached path
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns>System.String.</returns>
        private async Task<string> GetCroppedImage(string originalImagePath, DateTime dateModified)
        {
            var name = originalImagePath;
            name += "datemodified=" + dateModified.Ticks;

            var croppedImagePath = CroppedImageCache.GetResourcePath(name, Path.GetExtension(originalImagePath));

            var semaphore = GetLock(croppedImagePath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Check again in case of contention
            if (File.Exists(croppedImagePath))
            {
                semaphore.Release();
                return croppedImagePath;
            }

            try
            {
                using (var fileStream = new FileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
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
                                var parentPath = Path.GetDirectoryName(croppedImagePath);

                                if (!Directory.Exists(parentPath))
                                {
                                    Directory.CreateDirectory(parentPath);
                                }

                                using (var outputStream = new FileStream(croppedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read))
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

                return originalImagePath;
            }
            finally
            {
                semaphore.Release();
            }

            return croppedImagePath;
        }

        /// <summary>
        /// Gets the enhanced image.
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task<string> GetEnhancedImage(string originalImagePath, DateTime dateModified, BaseItem item, ImageType imageType, int imageIndex)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var supportedImageEnhancers = ImageEnhancers.Where(i =>
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

            }).ToList();

            return GetEnhancedImage(originalImagePath, dateModified, item, imageType, imageIndex, supportedImageEnhancers);
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
        public async Task<string> GetEnhancedImage(string originalImagePath, DateTime dateModified, BaseItem item, ImageType imageType, int imageIndex, List<IImageEnhancer> supportedEnhancers)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var cacheGuid = GetImageCacheTag(originalImagePath, dateModified, supportedEnhancers, item, imageType);

            // All enhanced images are saved as png to allow transparency
            var enhancedImagePath = EnhancedImageCache.GetResourcePath(cacheGuid + ".png");

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
                using (var fileStream = new FileStream(originalImagePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
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

                                if (!Directory.Exists(parentDirectory))
                                {
                                    Directory.CreateDirectory(parentDirectory);
                                }

                                //And then save it in the cache
                                using (var outputStream = new FileStream(enhancedImagePath, FileMode.Create, FileAccess.Write, FileShare.Read))
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
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Guid GetImageCacheTag(BaseItem item, ImageType imageType, string imagePath)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            var dateModified = GetImageDateModified(item, imagePath);

            var supportedEnhancers = ImageEnhancers.Where(i =>
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

            }).ToList();

            return GetImageCacheTag(imagePath, dateModified, supportedEnhancers, item, imageType);
        }

        /// <summary>
        /// Gets the image cache tag.
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified of the original image file.</param>
        /// <param name="imageEnhancers">The image enhancers.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Guid GetImageCacheTag(string originalImagePath, DateTime dateModified, IEnumerable<IImageEnhancer> imageEnhancers, BaseItem item, ImageType imageType)
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

            // Cache name is created with supported enhancers combined with the last config change so we pick up new config changes
            var cacheKeys = imageEnhancers.Select(i => i.GetConfigurationCacheKey(item, imageType)).ToList();
            cacheKeys.Add(originalImagePath + dateModified.Ticks);

            return string.Join("|", cacheKeys.ToArray()).GetMD5();
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
        private async Task<Image> ExecuteImageEnhancers(IEnumerable<IImageEnhancer> imageEnhancers, Image originalImage, BaseItem item, ImageType imageType, int imageIndex)
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
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
    }
}
