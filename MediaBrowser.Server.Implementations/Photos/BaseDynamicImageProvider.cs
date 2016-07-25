using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Server.Implementations.Photos
{
    public abstract class BaseDynamicImageProvider<T> : IHasItemChangeMonitor, IForcedProvider, ICustomMetadataProvider<T>, IHasOrder
        where T : IHasMetadata
    {
        protected IFileSystem FileSystem { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }
        protected IApplicationPaths ApplicationPaths { get; private set; }
        protected IImageProcessor ImageProcessor { get; set; }

        protected BaseDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor)
        {
            ApplicationPaths = applicationPaths;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
            ImageProcessor = imageProcessor;
        }

        protected virtual bool Supports(IHasImages item)
        {
            return true;
        }

        public virtual IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        private IEnumerable<ImageType> GetEnabledImages(IHasImages item)
        {
            //var options = ProviderManager.GetMetadataOptions(item);

            return GetSupportedImages(item);
            //return GetSupportedImages(item).Where(i => IsEnabled(options, i, item)).ToList();
        }

        private bool IsEnabled(MetadataOptions options, ImageType type, IHasImages item)
        {
            if (type == ImageType.Backdrop)
            {
                if (item.LockedFields.Contains(MetadataFields.Backdrops))
                {
                    return false;
                }
            }
            else if (type == ImageType.Screenshot)
            {
                if (item.LockedFields.Contains(MetadataFields.Screenshots))
                {
                    return false;
                }
            }
            else
            {
                if (item.LockedFields.Contains(MetadataFields.Images))
                {
                    return false;
                }
            }

            return options.IsEnabled(type);
        }

        public async Task<ItemUpdateType> FetchAsync(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (!Supports(item))
            {
                return ItemUpdateType.None;
            }

            var updateType = ItemUpdateType.None;
            var supportedImages = GetEnabledImages(item).ToList();

            if (supportedImages.Contains(ImageType.Primary))
            {
                var primaryResult = await FetchAsync(item, ImageType.Primary, options, cancellationToken).ConfigureAwait(false);
                updateType = updateType | primaryResult;
            }

            if (supportedImages.Contains(ImageType.Thumb))
            {
                var thumbResult = await FetchAsync(item, ImageType.Thumb, options, cancellationToken).ConfigureAwait(false);
                updateType = updateType | thumbResult;
            }

            return updateType;
        }

        protected async Task<ItemUpdateType> FetchAsync(IHasImages item, ImageType imageType, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var image = item.GetImageInfo(imageType, 0);

            if (image != null)
            {
                if (!image.IsLocalFile)
                {
                    return ItemUpdateType.None;
                }

                if (!FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
                {
                    return ItemUpdateType.None;
                }
            }

            var items = await GetItemsWithImages(item).ConfigureAwait(false);

            return await FetchToFileInternal(item, items, imageType, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<ItemUpdateType> FetchToFileInternal(IHasImages item,
            List<BaseItem> itemsWithImages,
            ImageType imageType,
            CancellationToken cancellationToken)
        {
            var outputPathWithoutExtension = Path.Combine(ApplicationPaths.TempDirectory, Guid.NewGuid().ToString("N"));
            FileSystem.CreateDirectory(Path.GetDirectoryName(outputPathWithoutExtension));
            string outputPath = await CreateImage(item, itemsWithImages, outputPathWithoutExtension, imageType, 0).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return ItemUpdateType.None;
            }

            await ProviderManager.SaveImage(item, outputPath, "image/png", imageType, null, Guid.NewGuid().ToString("N"), cancellationToken).ConfigureAwait(false);

            return ItemUpdateType.ImageUpdate;
        }

        protected abstract Task<List<BaseItem>> GetItemsWithImages(IHasImages item);

        protected Task<string> CreateThumbCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 640, 360);
        }

        protected virtual IEnumerable<string> GetStripCollageImagePaths(IHasImages primaryItem, IEnumerable<BaseItem> items)
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
                .Where(i => !string.IsNullOrWhiteSpace(i));
        }

        protected Task<string> CreatePosterCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 400, 600);
        }

        protected Task<string> CreateSquareCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath)
        {
            return CreateCollage(primaryItem, items, outputPath, 600, 600);
        }

        protected Task<string> CreateThumbCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, int width, int height)
        {
            return CreateCollage(primaryItem, items, outputPath, width, height);
        }

        private async Task<string> CreateCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, int width, int height)
        {
            FileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

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

            await ImageProcessor.CreateImageCollage(options).ConfigureAwait(false);
            return outputPath;
        }

        public string Name
        {
            get { return "Dynamic Image Provider"; }
        }

        protected virtual async Task<string> CreateImage(IHasImages item,
            List<BaseItem> itemsWithImages,
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
                return await CreateThumbCollage(item, itemsWithImages, outputPath).ConfigureAwait(false);
            }

            if (imageType == ImageType.Primary)
            {
                if (item is UserView)
                {
                    return await CreateSquareCollage(item, itemsWithImages, outputPath).ConfigureAwait(false);
                }
                if (item is Playlist || item is MusicGenre)
                {
                    return await CreateSquareCollage(item, itemsWithImages, outputPath).ConfigureAwait(false);
                }
                return await CreatePosterCollage(item, itemsWithImages, outputPath).ConfigureAwait(false);
            }

            throw new ArgumentException("Unexpected image type");
        }

        protected virtual int MaxImageAgeDays
        {
            get { return 7; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryServicee)
        {
            if (!Supports(item))
            {
                return false;
            }

            var supportedImages = GetEnabledImages(item).ToList();

            if (supportedImages.Contains(ImageType.Primary) && HasChanged(item, ImageType.Primary))
            {
                return true;
            }
            if (supportedImages.Contains(ImageType.Thumb) && HasChanged(item, ImageType.Thumb))
            {
                return true;
            }

            return false;
        }

        protected bool HasChanged(IHasImages item, ImageType type)
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

                var age = DateTime.UtcNow - image.DateModified;
                if (age.TotalDays <= MaxImageAgeDays)
                {
                    return false;
                }
            }

            return true;
        }

        protected List<BaseItem> GetFinalItems(List<BaseItem> items)
        {
            return GetFinalItems(items, 4);
        }

        protected virtual List<BaseItem> GetFinalItems(List<BaseItem> items, int limit)
        {
            // Rotate the images once every x days
            var random = DateTime.Now.DayOfYear % MaxImageAgeDays;

            return items
                .OrderBy(i => (random + string.Empty + items.IndexOf(i)).GetMD5())
                .Take(limit)
                .OrderBy(i => i.Name)
                .ToList();
        }

        public int Order
        {
            get
            {
                // Run before the default image provider which will download placeholders
                return 0;
            }
        }

        protected async Task<string> CreateSingleImage(List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType)
        {
            var image = itemsWithImages
                .Where(i => i.HasImage(imageType) && i.GetImageInfo(imageType, 0).IsLocalFile && Path.HasExtension(i.GetImagePath(imageType)))
                .Select(i => i.GetImagePath(imageType))
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(image))
            {
                return null;
            }

            var ext = Path.GetExtension(image);

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ext);
            File.Copy(image, outputPath);

            return outputPath;
        }
    }
}
