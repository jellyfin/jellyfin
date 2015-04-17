using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
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

namespace MediaBrowser.Server.Implementations.Photos
{
    public abstract class BaseDynamicImageProvider<T> : IHasChangeMonitor, IForcedProvider, ICustomMetadataProvider<T>, IHasOrder
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

        public virtual bool Supports(IHasImages item)
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

        public async Task<ItemUpdateType> FetchAsync(T item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            if (!Supports(item))
            {
                return ItemUpdateType.None;
            }

            var primaryResult = await FetchAsync(item, ImageType.Primary, options, cancellationToken).ConfigureAwait(false);
            var thumbResult = await FetchAsync(item, ImageType.Thumb, options, cancellationToken).ConfigureAwait(false);

            return primaryResult | thumbResult;
        }

        protected async Task<ItemUpdateType> FetchAsync(IHasImages item, ImageType imageType, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var items = await GetItemsWithImages(item).ConfigureAwait(false);
            var cacheKey = GetConfigurationCacheKey(items, item.Name);

            if (!HasChanged(item, imageType, cacheKey))
            {
                return ItemUpdateType.None;
            }

            return await FetchToFileInternal(item, items, imageType, cacheKey, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<ItemUpdateType> FetchToFileInternal(IHasImages item,
            List<BaseItem> itemsWithImages,
            ImageType imageType,
            string cacheKey,
            CancellationToken cancellationToken)
        {
            var outputPath = Path.Combine(ApplicationPaths.TempDirectory, Guid.NewGuid() + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var imageCreated = CreateImage(item, itemsWithImages, outputPath, imageType, 0);

            if (!imageCreated)
            {
                return ItemUpdateType.None;
            }

            await ProviderManager.SaveImage(item, outputPath, "image/png", imageType, null, cacheKey, cancellationToken).ConfigureAwait(false);

            return ItemUpdateType.ImageUpdate;
        }

        protected abstract Task<List<BaseItem>> GetItemsWithImages(IHasImages item);

        private const string Version = "32";
        protected string GetConfigurationCacheKey(List<BaseItem> items, string itemName)
        {
            var parts = Version + "_" + (itemName ?? string.Empty) + "_" +
                        string.Join(",", items.Select(i => i.Id.ToString("N")).ToArray());

            return parts.GetMD5().ToString("N");
        }

        protected void CreateThumbCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, bool drawText)
        {
            CreateCollage(primaryItem, items, outputPath, 960, 540, drawText, primaryItem.Name);
        }

        protected virtual IEnumerable<string> GetStripCollageImagePaths(IHasImages primaryItem, IEnumerable<BaseItem> items)
        {
            return items
                .Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb))
                .Where(i => !string.IsNullOrWhiteSpace(i));
        }

        protected void CreatePosterCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath)
        {
            CreateCollage(primaryItem, items, outputPath, 600, 900, true, primaryItem.Name);
        }

        protected void CreateSquareCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, bool drawText)
        {
            CreateCollage(primaryItem, items, outputPath, 800, 800, drawText, primaryItem.Name);
        }

        protected void CreateThumbCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, int width, int height, bool drawText, string text)
        {
            CreateCollage(primaryItem, items, outputPath, width, height, drawText, text);
        }

        private void CreateCollage(IHasImages primaryItem, List<BaseItem> items, string outputPath, int width, int height, bool drawText, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new ImageCollageOptions
            {
                Height = height,
                Width = width,
                OutputPath = outputPath,
                Text = drawText ? text : null,
                InputPaths = GetStripCollageImagePaths(primaryItem, items).ToArray()
            };

            ImageProcessor.CreateImageCollage(options);
        }

        public string Name
        {
            get { return "Dynamic Image Provider"; }
        }

        protected virtual bool CreateImage(IHasImages item,
            List<BaseItem> itemsWithImages,
            string outputPath,
            ImageType imageType,
            int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return false;
            }

            var drawText = !(item is UserView);

            if (imageType == ImageType.Thumb)
            {
                CreateThumbCollage(item, itemsWithImages, outputPath, drawText);
                return true;
            }

            if (imageType == ImageType.Primary)
            {
                if (item is UserView)
                {
                    CreateSquareCollage(item, itemsWithImages, outputPath, drawText);
                }
                else if (item is PhotoAlbum || item is Playlist)
                {
                    CreateSquareCollage(item, itemsWithImages, outputPath, drawText);
                }
                else
                {
                    CreatePosterCollage(item, itemsWithImages, outputPath);
                }

                return true;
            }

            throw new ArgumentException("Unexpected image type");
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (!Supports(item))
            {
                return false;
            }

            var items = GetItemsWithImages(item).Result;
            var cacheKey = GetConfigurationCacheKey(items, item.Name);

            return HasChanged(item, ImageType.Primary, cacheKey) || HasChanged(item, ImageType.Thumb, cacheKey);
        }

        protected bool HasChanged(IHasImages item, ImageType type, string cacheKey)
        {
            var image = item.GetImageInfo(type, 0);

            if (image != null)
            {
                if (!FileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
                {
                    return false;
                }

                var currentPathCacheKey = (Path.GetFileNameWithoutExtension(image.Path) ?? string.Empty).Split('_').LastOrDefault();

                if (string.Equals(cacheKey, currentPathCacheKey, StringComparison.OrdinalIgnoreCase))
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
            var random = DateTime.Now.DayOfYear % 4;

            return items
                .OrderBy(i => (random + "" + items.IndexOf(i)).GetMD5())
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
    }
}
