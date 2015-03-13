using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
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
    public abstract class BaseDynamicImageProvider<T> : IHasChangeMonitor, IForcedProvider, IDynamicImageProvider, IHasOrder
        where T : IHasMetadata
    {
        protected IFileSystem FileSystem { get; private set; }
        protected IProviderManager ProviderManager { get; private set; }
        protected IApplicationPaths ApplicationPaths { get; private set; }

        protected BaseDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths)
        {
            ApplicationPaths = applicationPaths;
            ProviderManager = providerManager;
            FileSystem = fileSystem;
        }

        public virtual bool Supports(IHasImages item)
        {
            return item is T;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        public async Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var items = await GetItemsWithImages(item).ConfigureAwait(false);
            var cacheKey = GetConfigurationCacheKey(items);

            var result = await FetchAsyncInternal(item, items, type, cacheKey, cancellationToken).ConfigureAwait(false);

            return new DynamicImageResponse
            {
                HasImage = result != null,
                Stream = result,
                InternalCacheKey = cacheKey,
                Format = ImageFormat.Png
            };
        }

        protected abstract Task<List<BaseItem>> GetItemsWithImages(IHasImages item);

        private const string Version = "3";
        protected string GetConfigurationCacheKey(List<BaseItem> items)
        {
            return (Version + "_" + string.Join(",", items.Select(i => i.Id.ToString("N")).ToArray())).GetMD5().ToString("N");
        }

        protected Task<Stream> FetchAsyncInternal(IHasImages item,
            List<BaseItem> itemsWithImages,
            ImageType imageType,
            string cacheKey,
            CancellationToken cancellationToken)
        {
            return CreateImageAsync(item, itemsWithImages, imageType, 0);
        }

        protected Task<Stream> GetThumbCollage(List<BaseItem> items)
        {
            var files = items
                .Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            return DynamicImageHelpers.GetThumbCollage(files,
                FileSystem,
                1600,
                900,
                ApplicationPaths);
        }

        protected Task<Stream> GetSquareCollage(List<BaseItem> items)
        {
            var files = items
                .Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb))
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            return DynamicImageHelpers.GetSquareCollage(files,
                FileSystem,
                800, ApplicationPaths);
        }

        public string Name
        {
            get { return "Dynamic Image Provider"; }
        }

        protected virtual async Task<Stream> CreateImageAsync(IHasImages item,
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
            return GetFinalItems(items, 4);
        }

        protected List<BaseItem> GetFinalItems(List<BaseItem> items, int limit)
        {
            // Rotate the images no more than once per week
            var random = new Random(GetWeekOfYear()).Next();

            return items
                .OrderBy(i => random - items.IndexOf(i))
                .Take(limit)
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
