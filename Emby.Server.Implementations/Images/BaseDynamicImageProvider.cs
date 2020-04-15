using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.Images
{
    /// <summary>
    /// A dynamic image provider.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public abstract class BaseDynamicImageProvider<T> : IHasItemChangeMonitor, IForcedProvider, ICustomMetadataProvider<T>, IHasOrder
        where T : BaseItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDynamicImageProvider{T}"/> class.
        /// </summary>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="imageProcessor">The image processor.</param>
        protected BaseDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
        {
            ApplicationPaths = applicationPaths;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
            ImageProcessor = imageProcessor;
        }

        /// <summary>
        /// Gets the filesystem.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets the provider manager.
        /// </summary>
        protected IProviderManager ProviderManager { get; }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        protected IApplicationPaths ApplicationPaths { get; }

        /// <summary>
        /// Gets and sets the image processor.
        /// </summary>
        protected IImageProcessor ImageProcessor { get; set; }

        /// <summary>
        /// Gets the supported images.
        /// </summary>
        protected virtual IReadOnlyCollection<ImageType> SupportedImages { get; }
            = new[] { ImageType.Primary };

        /// <inheritdoc />
        public string Name => "Dynamic Image Provider";

        /// <summary>
        /// Gets the maximum image age in days.
        /// </summary>
        protected virtual int MaxImageAgeDays => 7;

        /// <inheritdoc />
        public int Order => 0;

        /// <summary>
        /// Returns whether or not this supports the provided item.
        /// </summary>
        /// <param name="_">The item.</param>
        /// <returns>Whether or not it is supported.</returns>
        protected virtual bool Supports(BaseItem _) => true;

        /// <inheritdoc />
        public async Task<ItemUpdateType> FetchAsync(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (!Supports(item))
            {
                return ItemUpdateType.None;
            }

            var updateType = ItemUpdateType.None;

            if (SupportedImages.Contains(ImageType.Primary))
            {
                var primaryResult = await FetchAsync(item, ImageType.Primary, options, cancellationToken).ConfigureAwait(false);
                updateType = updateType | primaryResult;
            }

            if (SupportedImages.Contains(ImageType.Thumb))
            {
                var thumbResult = await FetchAsync(item, ImageType.Thumb, options, cancellationToken).ConfigureAwait(false);
                updateType = updateType | thumbResult;
            }

            return updateType;
        }

        /// <summary>
        /// Fetches the
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageType">The image type.</param>
        /// <param name="options">The metadata refresh options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task </returns>
        protected Task<ItemUpdateType> FetchAsync(BaseItem item, ImageType imageType, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var image = item.GetImageInfo(imageType, 0);

            if (image != null)
            {
                if (!image.IsLocalFile)
                {
                    return Task.FromResult(ItemUpdateType.None);
                }

                if (!FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
                {
                    return Task.FromResult(ItemUpdateType.None);
                }
            }

            var items = GetItemsWithImages(item);

            return FetchToFileInternal(item, items, imageType, cancellationToken);
        }

        /// <summary>
        /// Updates the specified images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemsWithImages">The items with images.</param>
        /// <param name="imageType">The image type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        protected async Task<ItemUpdateType> FetchToFileInternal(
            BaseItem item,
            IReadOnlyList<BaseItem> itemsWithImages,
            ImageType imageType,
            CancellationToken cancellationToken)
        {
            var outputPathWithoutExtension = Path.Combine(ApplicationPaths.TempDirectory, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPathWithoutExtension));
            string outputPath = CreateImage(item, itemsWithImages, outputPathWithoutExtension, imageType, 0);

            if (string.IsNullOrEmpty(outputPath))
            {
                return ItemUpdateType.None;
            }

            var mimeType = MimeTypes.GetMimeType(outputPath);

            if (string.Equals(mimeType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                mimeType = "image/png";
            }

            await ProviderManager.SaveImage(item, outputPath, mimeType, imageType, null, false, cancellationToken).ConfigureAwait(false);

            return ItemUpdateType.ImageUpdate;
        }

        /// <summary>
        /// Returns a read-only list of items with images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>A read-only list containing items with images.</returns>
        protected abstract IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item);

        /// <summary>
        /// Creates a thumbnail collage.
        /// </summary>
        /// <param name="primaryItem">The primary item.</param>
        /// <param name="items">The items.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns>The resulting path, or null.</returns>
        protected string CreateThumbCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 640, 360);
        }

        /// <summary>
        /// Gets the collage image paths.
        /// </summary>
        /// <param name="primaryItem">The primary item.</param>
        /// <param name="items">The items.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the image paths.</returns>
        protected virtual IEnumerable<string> GetStripCollageImagePaths(BaseItem primaryItem, IEnumerable<BaseItem> items)
        {
            return items
                .Select(i =>
                {
                    var image = i.GetImageInfo(ImageType.Primary, 0);
                    if (image != null && image.IsLocalFile)
                    {
                        return image.Path;
                    }

                    image = i.GetImageInfo(ImageType.Thumb, 0);
                    if (image != null && image.IsLocalFile)
                    {
                        return image.Path;
                    }

                    return null;
                })
                .Where(i => !string.IsNullOrEmpty(i));
        }

        /// <summary>
        /// Creates a poster collage.
        /// </summary>
        /// <param name="primaryItem">The primary item.</param>
        /// <param name="items">The items.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns>The resulting path, or null.</returns>
        protected string CreatePosterCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 400, 600);
        }

        /// <summary>
        /// Creates a square collage.
        /// </summary>
        /// <param name="primaryItem">The primary item.</param>
        /// <param name="items">The items.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns>The resulting path, or null.</returns>
        protected string CreateSquareCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 600, 600);
        }

        /// <summary>
        /// Creates a thumbnail collage.
        /// </summary>
        /// <param name="primaryItem">The primary item.</param>
        /// <param name="items">The items.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <returns>The resulting path, or null.</returns>
        protected string CreateThumbCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath, int width, int height)
        {
            return CreateCollage(primaryItem, items, outputPath, width, height);
        }

        private string CreateCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath, int width, int height)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new ImageCollageOptions
            {
                Height = height,
                Width = width,
                OutputPath = outputPath,
                InputPaths = GetStripCollageImagePaths(primaryItem, items).ToArray()
            };

            if (options.InputPaths.Length == 0)
            {
                return null;
            }

            if (!ImageProcessor.SupportsImageCollageCreation)
            {
                return null;
            }

            ImageProcessor.CreateImageCollage(options);
            return outputPath;
        }

        /// <summary>
        /// Creates an image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemsWithImages">The items with the images.</param>
        /// <param name="outputPathWithoutExtension">The output path without an extension.</param>
        /// <param name="imageType">The image type.</param>
        /// <param name="imageIndex">The image index.</param>
        /// <returns>The resulting image's path, or null.</returns>
        /// <exception cref="ArgumentException">If the image type is unsupported.</exception>
        protected virtual string CreateImage(BaseItem item,
            IReadOnlyCollection<BaseItem> itemsWithImages,
            string outputPathWithoutExtension,
            ImageType imageType,
            int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            string outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            if (imageType == ImageType.Thumb)
            {
                return CreateThumbCollage(item, itemsWithImages, outputPath);
            }

            if (imageType == ImageType.Primary)
            {
                if (item is UserView || item is Playlist || item is MusicGenre || item is Genre || item is PhotoAlbum)
                {
                    return CreateSquareCollage(item, itemsWithImages, outputPath);
                }

                return CreatePosterCollage(item, itemsWithImages, outputPath);
            }

            throw new ArgumentException("Unexpected image type", nameof(imageType));
        }

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryServicee)
        {
            if (!Supports(item))
            {
                return false;
            }

            if (SupportedImages.Contains(ImageType.Primary) && HasChanged(item, ImageType.Primary))
            {
                return true;
            }
            if (SupportedImages.Contains(ImageType.Thumb) && HasChanged(item, ImageType.Thumb))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the item's image has changed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The image type.</param>
        /// <returns>Whether the image has changed.</returns>
        protected bool HasChanged(BaseItem item, ImageType type)
        {
            var image = item.GetImageInfo(type, 0);

            if (image == null)
            {
                return true;
            }

            return image.IsLocalFile
                   && FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path)
                   && HasChangedByDate(item, image);
        }

        /// <summary>
        /// Returns whether the provided image is older than <see cref="MaxImageAgeDays"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="image">The image.</param>
        /// <returns>Whether the image is older than <see cref="MaxImageAgeDays"/>.</returns>
        protected virtual bool HasChangedByDate(BaseItem item, ItemImageInfo image)
        {
            var age = DateTime.UtcNow - image.DateModified;
            return age.TotalDays > MaxImageAgeDays;
        }

        /// <summary>
        /// Creates an image based on the provided items at the specified path.
        /// </summary>
        /// <param name="itemsWithImages">The items.</param>
        /// <param name="outputPathWithoutExtension">The output path without the file extension.</param>
        /// <param name="imageType">The image type.</param>
        /// <returns>The output path of the created image, or null.</returns>
        protected string CreateSingleImage(IEnumerable<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType)
        {
            var image = itemsWithImages
                .Where(i => i.HasImage(imageType) && i.GetImageInfo(imageType, 0).IsLocalFile && Path.HasExtension(i.GetImagePath(imageType)))
                .Select(i => i.GetImagePath(imageType))
                .FirstOrDefault();

            if (string.IsNullOrEmpty(image))
            {
                return null;
            }

            var ext = Path.GetExtension(image);

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ext);
            File.Copy(image, outputPath, true);

            return outputPath;
        }
    }
}
