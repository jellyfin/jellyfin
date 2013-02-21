using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Class ImageManager
    /// </summary>
    public class ImageManager : BaseManager<Kernel>
    {
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
        private readonly ConcurrentDictionary<string, Task<ImageSize>> _cachedImagedSizes = new ConcurrentDictionary<string, Task<ImageSize>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public ImageManager(Kernel kernel)
            : base(kernel)
        {
            ImageSizeCache = new FileSystemRepository(Path.Combine(Kernel.ApplicationPaths.ImageCachePath, "image-sizes"));
            ResizedImageCache = new FileSystemRepository(Path.Combine(Kernel.ApplicationPaths.ImageCachePath, "resized-images"));
            CroppedImageCache = new FileSystemRepository(Path.Combine(Kernel.ApplicationPaths.ImageCachePath, "cropped-images"));
            EnhancedImageCache = new FileSystemRepository(Path.Combine(Kernel.ApplicationPaths.ImageCachePath, "enhanced-images"));
        }

        /// <summary>
        /// Processes an image by resizing to target dimensions
        /// </summary>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        /// <param name="cropWhitespace">if set to <c>true</c> [crop whitespace].</param>
        /// <param name="dateModified">The last date modified of the original image file</param>
        /// <param name="toStream">The stream to save the new image to</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">entity</exception>
        public async Task ProcessImage(BaseItem entity, ImageType imageType, int imageIndex, bool cropWhitespace, DateTime dateModified, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            if (toStream == null)
            {
                throw new ArgumentNullException("toStream");
            }

            var originalImagePath = GetImagePath(entity, imageType, imageIndex);

            if (cropWhitespace)
            {
                try
                {
                    originalImagePath = GetCroppedImage(originalImagePath, dateModified);
                }
                catch (Exception ex)
                {
                    // We have to have a catch-all here because some of the .net image methods throw a plain old Exception
                    Logger.ErrorException("Error cropping image", ex);
                }
            }

            try
            {
                // Enhance if we have enhancers
                var ehnancedImagePath = await GetEnhancedImage(originalImagePath, dateModified, entity, imageType, imageIndex).ConfigureAwait(false);

                // If the path changed update dateModified
                if (!ehnancedImagePath.Equals(originalImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    dateModified = File.GetLastWriteTimeUtc(ehnancedImagePath);
                    originalImagePath = ehnancedImagePath;
                }
            }
            catch
            {
                Logger.Error("Error enhancing image");
            }

            var originalImageSize = await GetImageSize(originalImagePath, dateModified).ConfigureAwait(false);

            // Determine the output size based on incoming parameters
            var newSize = DrawingUtils.Resize(originalImageSize, width, height, maxWidth, maxHeight);

            if (!quality.HasValue)
            {
                quality = 90;
            }

            var cacheFilePath = GetCacheFilePath(originalImagePath, newSize, quality.Value, dateModified);

            // Grab the cache file if it already exists
            try
            {
                using (var fileStream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await fileStream.CopyToAsync(toStream).ConfigureAwait(false);
                }
                return;
            }
            catch (FileNotFoundException)
            {
                // Cache file doesn't exist. No biggie.
            }

            using (var fileStream = File.OpenRead(originalImagePath))
            {
                using (var originalImage = Bitmap.FromStream(fileStream, true, false))
                {
                    var newWidth = Convert.ToInt32(newSize.Width);
                    var newHeight = Convert.ToInt32(newSize.Height);

                    // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
                    var thumbnail = originalImage.PixelFormat.HasFlag(PixelFormat.Indexed) ? new Bitmap(originalImage, newWidth, newHeight) : new Bitmap(newWidth, newHeight, originalImage.PixelFormat);

                    // Preserve the original resolution
                    thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

                    var thumbnailGraph = Graphics.FromImage(thumbnail);

                    thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                    thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                    thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    thumbnailGraph.CompositingMode = CompositingMode.SourceOver;

                    thumbnailGraph.DrawImage(originalImage, 0, 0, newWidth, newHeight);

                    var outputFormat = originalImage.RawFormat;

                    using (var memoryStream = new MemoryStream { })
                    {
                        // Save to the memory stream
                        thumbnail.Save(outputFormat, memoryStream, quality.Value);

                        var bytes = memoryStream.ToArray();

                        var outputTask = Task.Run(async () => await toStream.WriteAsync(bytes, 0, bytes.Length));

                        // Save to the cache location
                        using (var cacheFileStream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                        {
                            // Save to the filestream
                            await cacheFileStream.WriteAsync(bytes, 0, bytes.Length);
                        }

                        await outputTask.ConfigureAwait(false);
                    }

                    thumbnailGraph.Dispose();
                    thumbnail.Dispose();
                }
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
        public Task<ImageSize> GetImageSize(string imagePath, DateTime dateModified)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            var name = imagePath + "datemodified=" + dateModified.Ticks;

            return _cachedImagedSizes.GetOrAdd(name, keyName => GetImageSizeTask(keyName, imagePath));
        }

        /// <summary>
        /// Gets cached image dimensions, or results null if non-existant
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>Task{ImageSize}.</returns>
        private Task<ImageSize> GetImageSizeTask(string keyName, string imagePath)
        {
            return Task.Run(() => GetImageSize(keyName, imagePath));
        }

        /// <summary>
        /// Gets the size of the image.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="imagePath">The image path.</param>
        /// <returns>ImageSize.</returns>
        private ImageSize GetImageSize(string keyName, string imagePath)
        {
            // Now check the file system cache
            var fullCachePath = ImageSizeCache.GetResourcePath(keyName, ".pb");

            try
            {
                var result = Kernel.ProtobufSerializer.DeserializeFromFile<int[]>(fullCachePath);

                return new ImageSize { Width = result[0], Height = result[1] };
            }
            catch (FileNotFoundException)
            {
                // Cache file doesn't exist no biggie
            }

            var size = ImageHeader.GetDimensions(imagePath);

            var imageSize = new ImageSize { Width = size.Width, Height = size.Height };

            // Update the file system cache
            CacheImageSize(fullCachePath, size.Width, size.Height);

            return imageSize;
        }

        /// <summary>
        /// Caches image dimensions
        /// </summary>
        /// <param name="cachePath">The cache path.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        private void CacheImageSize(string cachePath, int width, int height)
        {
            var output = new[] { width, height };

            Kernel.ProtobufSerializer.SerializeToFile(output, cachePath);
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

            if (imageType == ImageType.ChapterImage)
            {
                var video = (Video)item;

                if (video.Chapters == null)
                {
                    throw new InvalidOperationException(string.Format("Item {0} does not have any Chapters.", item.Name));
                }

                return video.Chapters[imageIndex].ImagePath;
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
            if (!metaFileEntry.HasValue)
            {
                var episode = item as Episode;

                if (episode != null && episode.Season != null)
                {
                    episode.Season.ResolveArgs.GetMetaFileByPath(imagePath);
                }
            }

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            return metaFileEntry == null ? File.GetLastWriteTimeUtc(imagePath) : metaFileEntry.Value.LastWriteTimeUtc;
        }

        /// <summary>
        /// Crops whitespace from an image, caches the result, and returns the cached path
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns>System.String.</returns>
        private string GetCroppedImage(string originalImagePath, DateTime dateModified)
        {
            var name = originalImagePath;
            name += "datemodified=" + dateModified.Ticks;

            var croppedImagePath = CroppedImageCache.GetResourcePath(name, Path.GetExtension(originalImagePath));

            if (!CroppedImageCache.ContainsFilePath(croppedImagePath))
            {
                using (var fileStream = File.OpenRead(originalImagePath))
                {
                    using (var originalImage = (Bitmap)Bitmap.FromStream(fileStream, true, false))
                    {
                        var outputFormat = originalImage.RawFormat;

                        using (var croppedImage = originalImage.CropWhitespace())
                        {
                            using (var cacheFileStream = new FileStream(croppedImagePath, FileMode.Create))
                            {
                                croppedImage.Save(outputFormat, cacheFileStream, 100);
                            }
                        }
                    }
                }
            }

            return croppedImagePath;
        }

        /// <summary>
        /// Runs an image through the image enhancers, caches the result, and returns the cached path
        /// </summary>
        /// <param name="originalImagePath">The original image path.</param>
        /// <param name="dateModified">The date modified of the original image file.</param>
        /// <param name="item">The item.</param>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">originalImagePath</exception>
        public async Task<string> GetEnhancedImage(string originalImagePath, DateTime dateModified, BaseItem item, ImageType imageType, int imageIndex)
        {
            if (string.IsNullOrEmpty(originalImagePath))
            {
                throw new ArgumentNullException("originalImagePath");
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var supportedEnhancers = Kernel.ImageEnhancers.Where(i => i.Supports(item, imageType)).ToList();

            // No enhancement - don't cache
            if (supportedEnhancers.Count == 0)
            {
                return originalImagePath;
            }

            var cacheGuid = GetImageCacheTag(originalImagePath, dateModified, supportedEnhancers, item, imageType);

            // All enhanced images are saved as png to allow transparency
            var enhancedImagePath = EnhancedImageCache.GetResourcePath(cacheGuid + ".png");

            if (!EnhancedImageCache.ContainsFilePath(enhancedImagePath))
            {
                using (var fileStream = File.OpenRead(originalImagePath))
                {
                    using (var originalImage = Image.FromStream(fileStream, true, false))
                    {
                        //Pass the image through registered enhancers
                        using (var newImage = await ExecuteImageEnhancers(supportedEnhancers, originalImage, item, imageType, imageIndex).ConfigureAwait(false))
                        {
                            //And then save it in the cache
                            newImage.Save(enhancedImagePath, ImageFormat.Png);
                        }
                    }
                }
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

            var supportedEnhancers = Kernel.ImageEnhancers.Where(i => i.Supports(item, imageType));

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
        public Guid GetImageCacheTag(string originalImagePath, DateTime dateModified, IEnumerable<BaseImageEnhancer> imageEnhancers, BaseItem item, ImageType imageType)
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
            var cacheKeys = imageEnhancers.Select(i => i.GetType().Name + i.LastConfigurationChange(item, imageType).Ticks).ToList();
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
        private async Task<Image> ExecuteImageEnhancers(IEnumerable<BaseImageEnhancer> imageEnhancers, Image originalImage, BaseItem item, ImageType imageType, int imageIndex)
        {
            var result = originalImage;

            // Run the enhancers sequentially in order of priority
            foreach (var enhancer in imageEnhancers)
            {
                result = await enhancer.EnhanceImageAsync(item, result, imageType, imageIndex).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                ImageSizeCache.Dispose();
                ResizedImageCache.Dispose();
                CroppedImageCache.Dispose();
                EnhancedImageCache.Dispose();
            }

            base.Dispose(dispose);
        }
    }
}
