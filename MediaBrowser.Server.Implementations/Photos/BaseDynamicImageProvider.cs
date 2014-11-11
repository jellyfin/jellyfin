using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    public abstract class BaseDynamicImageProvider<T> : IHasChangeMonitor
        where T : IHasImages
    {
        protected IFileSystem FileSystem { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }

        protected BaseDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager)
        {
            ProviderManager = providerManager;
            FileSystem = fileSystem;
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

        protected virtual bool Supports(IHasImages item)
        {
            return true;
        }

        protected abstract Task<List<BaseItem>> GetItemsWithImages(IHasImages item);

        private const string Version = "3";
        protected  string GetConfigurationCacheKey(List<BaseItem> items)
        {
            return (Version + "_" + string.Join(",", items.Select(i => i.Id.ToString("N")).ToArray())).GetMD5().ToString("N");
        }

        protected async Task<ItemUpdateType> FetchAsync(IHasImages item, ImageType imageType, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var items = await GetItemsWithImages(item).ConfigureAwait(false);
            var cacheKey = GetConfigurationCacheKey(items);

            if (!HasChanged(item, imageType, cacheKey))
            {
                return ItemUpdateType.None;
            }

            return await FetchAsyncInternal(item, items, imageType, cacheKey, options, cancellationToken).ConfigureAwait(false);
        }

        protected async Task<ItemUpdateType> FetchAsyncInternal(IHasImages item, 
            List<BaseItem> itemsWithImages,
            ImageType imageType, 
            string cacheKey, 
            MetadataRefreshOptions options, 
            CancellationToken cancellationToken)
        {
            var stream = await CreateImageAsync(item, itemsWithImages, imageType, 0).ConfigureAwait(false);

            if (stream == null)
            {
                return ItemUpdateType.None;
            }

            if (stream is MemoryStream)
            {
                using (stream)
                {
                    stream.Position = 0;

                    await ProviderManager.SaveImage(item, stream, "image/png", imageType, null, cacheKey, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms).ConfigureAwait(false);

                    ms.Position = 0;

                    await ProviderManager.SaveImage(item, ms, "image/png", imageType, null, cacheKey, cancellationToken).ConfigureAwait(false);
                }
            }

            return ItemUpdateType.ImageUpdate;
        }

        protected Task<Stream> GetThumbCollage(List<BaseItem> items)
        {
            return DynamicImageHelpers.GetThumbCollage(items.Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb)).ToList(),
                FileSystem,
                1600,
                900);
        }

        protected Task<Stream> GetSquareCollage(List<BaseItem> items)
        {
            return DynamicImageHelpers.GetSquareCollage(items.Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb)).ToList(),
                FileSystem,
                800);
        }

        public string Name
        {
            get { return "Dynamic Image Provider"; }
        }

        public async Task<Stream> CreateImageAsync(IHasImages item, 
            List<BaseItem> itemsWithImages,
            ImageType imageType, 
            int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            return imageType == ImageType.Thumb ?
                await GetThumbCollage(itemsWithImages).ConfigureAwait(false) :
                await GetSquareCollage(itemsWithImages).ConfigureAwait(false);
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            if (!Supports(item))
            {
                return false;
            }

            var items = GetItemsWithImages(item).Result;
            var cacheKey = GetConfigurationCacheKey(items);

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
            // Rotate the images no more than once per week
            var random = new Random(GetWeekOfYear()).Next();

            return items
                .OrderBy(i => random - items.IndexOf(i))
                .Take(4)
                .OrderBy(i => i.Name)
                .ToList();
        }

        private int GetWeekOfYear()
        {
            var usCulture = new CultureInfo("en-US");
            var weekNo = usCulture.Calendar.GetWeekOfYear(
                            DateTime.Now,
                            usCulture.DateTimeFormat.CalendarWeekRule,
                            usCulture.DateTimeFormat.FirstDayOfWeek);

            return weekNo;
        }
    }
}
