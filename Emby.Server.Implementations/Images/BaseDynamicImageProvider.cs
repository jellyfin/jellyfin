#pragma warning disable CS1591

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
    public abstract class BaseDynamicImageProvider<T> : IHasItemChangeMonitor, IForcedProvider, ICustomMetadataProvider<T>, IHasOrder
        where T : BaseItem
    {
        protected BaseDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
        {
            ApplicationPaths = applicationPaths;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
            ImageProcessor = imageProcessor;
        }

        protected IFileSystem FileSystem { get; }

        protected IProviderManager ProviderManager { get; }

        protected IApplicationPaths ApplicationPaths { get; }

        protected IImageProcessor ImageProcessor { get; set; }

        protected virtual IReadOnlyCollection<ImageType> SupportedImages { get; }
            = new ImageType[] { ImageType.Primary };

        /// <inheritdoc />
        public string Name => "Dynamic Image Provider";

        protected virtual int MaxImageAgeDays => 7;

        public int Order => 0;

        protected virtual bool Supports(BaseItem _) => true;

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

        protected abstract IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item);

        protected string CreateThumbCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 640, 360);
        }

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

        protected string CreatePosterCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 400, 600);
        }

        protected string CreateSquareCollage(BaseItem primaryItem, IEnumerable<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 600, 600);
        }

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

        protected virtual string CreateImage(
            BaseItem item,
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
                if (item is UserView
                    || item is Playlist
                    || item is MusicGenre
                    || item is Genre
                    || item is PhotoAlbum
                    || item is MusicArtist)
                {
                    return CreateSquareCollage(item, itemsWithImages, outputPath);
                }

                return CreatePosterCollage(item, itemsWithImages, outputPath);
            }

            throw new ArgumentException("Unexpected image type", nameof(imageType));
        }

        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
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

        protected bool HasChanged(BaseItem item, ImageType type)
        {
            var image = item.GetImageInfo(type, 0);

            if (image != null)
            {
                if (!image.IsLocalFile)
                {
                    return false;
                }

                if (!FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
                {
                    return false;
                }

                if (!HasChangedByDate(item, image))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool HasChangedByDate(BaseItem item, ItemImageInfo image)
        {
            var age = DateTime.UtcNow - image.DateModified;
            return age.TotalDays > MaxImageAgeDays;
        }

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
