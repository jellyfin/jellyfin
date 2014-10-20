using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MoreLinq;

namespace MediaBrowser.Server.Implementations.Photos
{
    public class PhotoAlbumImageProvider : ICustomMetadataProvider<PhotoAlbum>, IHasChangeMonitor
    {
        private readonly IFileSystem _fileSystem;
        private readonly IProviderManager _provider;

        public PhotoAlbumImageProvider(IFileSystem fileSystem, IProviderManager provider)
        {
            _fileSystem = fileSystem;
            _provider = provider;
        }

        public async Task<ItemUpdateType> FetchAsync(PhotoAlbum item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var primaryResult = await FetchAsync(item, ImageType.Primary, options, cancellationToken).ConfigureAwait(false);
            var thumbResult = await FetchAsync(item, ImageType.Thumb, options, cancellationToken).ConfigureAwait(false);

            return primaryResult | thumbResult;
        }

        private Task<ItemUpdateType> FetchAsync(IHasImages item, ImageType imageType, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var items = GetItemsWithImages(item);
            var cacheKey = GetConfigurationCacheKey(items);

            if (!HasChanged(item, imageType, cacheKey))
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            return FetchAsyncInternal(item, imageType, cacheKey, options, cancellationToken);
        }

        private async Task<ItemUpdateType> FetchAsyncInternal(IHasImages item, ImageType imageType, string cacheKey, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var img = await CreateImageAsync(item, imageType, 0).ConfigureAwait(false);

            if (img == null)
            {
                return ItemUpdateType.None;
            }

            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);

                ms.Position = 0;

                await _provider.SaveImage(item, ms, "image/png", imageType, null, cacheKey, cancellationToken).ConfigureAwait(false);
            }

            return ItemUpdateType.ImageUpdate;
        }

        private bool HasChanged(IHasImages item, ImageType type, string cacheKey)
        {
            var image = item.GetImageInfo(type, 0);

            if (image != null)
            {
                if (!_fileSystem.ContainsSubPath(item.GetInternalMetadataPath(), image.Path))
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

        private const string Version = "3";

        public string GetConfigurationCacheKey(List<BaseItem> items)
        {
            return (Version + "_" + string.Join(",", items.Select(i => i.Id.ToString("N")).ToArray())).GetMD5().ToString("N");
        }

        private List<BaseItem> GetItemsWithImages(IHasImages item)
        {
            var photoAlbum = item as PhotoAlbum;
            if (photoAlbum != null)
            {
                return GetFinalItems(photoAlbum.RecursiveChildren.Where(i => i is Photo).ToList());
            }

            var playlist = (Playlist)item;

            var items = playlist.GetManageableItems()
                .Select(i =>
                {
                    var subItem = i.Item2;

                    var episode = subItem as Episode;

                    if (episode != null)
                    {
                        var series = episode.Series;
                        if (series != null && series.HasImage(ImageType.Primary))
                        {
                            return series;
                        }
                    }

                    if (subItem.HasImage(ImageType.Primary))
                    {
                        return subItem;
                    }

                    var parent = subItem.Parent;

                    if (parent != null && parent.HasImage(ImageType.Primary))
                    {
                        if (parent is MusicAlbum)
                        {
                            return parent;
                        }
                    }

                    return null;
                })
                .Where(i => i != null)
                .DistinctBy(i => i.Id)
                .ToList();

            return GetFinalItems(items);
        }

        private List<BaseItem> GetFinalItems(List<BaseItem> items)
        {
            // Rotate the images no more than once per day
            var random = new Random(DateTime.Now.DayOfYear).Next();

            return items
                .OrderBy(i => random - items.IndexOf(i))
                .Take(4)
                .OrderBy(i => i.Name)
                .ToList();
        }

        public async Task<Image> CreateImageAsync(IHasImages item, ImageType imageType, int imageIndex)
        {
            var items = GetItemsWithImages(item);

            if (items.Count == 0)
            {
                return null;
            }

            return imageType == ImageType.Thumb ?
                await GetThumbCollage(items).ConfigureAwait(false) :
                await GetSquareCollage(items).ConfigureAwait(false);
        }

        private Task<Image> GetThumbCollage(List<BaseItem> items)
        {
            return GetThumbCollage(items.Select(i => i.GetImagePath(ImageType.Primary)).ToList());
        }

        private Task<Image> GetSquareCollage(List<BaseItem> items)
        {
            return GetSquareCollage(items.Select(i => i.GetImagePath(ImageType.Primary)).ToList());
        }

        private async Task<Image> GetThumbCollage(List<string> files)
        {
            if (files.Count < 3)
            {
                return await GetSingleImage(files).ConfigureAwait(false);
            }

            const int rows = 1;
            const int cols = 3;

            const int cellWidth = 2 * (ThumbImageWidth / 3);
            const int cellHeight = ThumbImageHeight;
            var index = 0;

            var img = new Bitmap(ThumbImageWidth, ThumbImageHeight, PixelFormat.Format32bppPArgb);

            using (var graphics = Graphics.FromImage(img))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceCopy;

                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * (cellWidth / 2);
                        var y = row * cellHeight;

                        if (files.Count > index)
                        {
                            using (var fileStream = _fileSystem.GetFileStream(files[index], FileMode.Open, FileAccess.Read, FileShare.Read, true))
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                                    memoryStream.Position = 0;

                                    using (var imgtemp = Image.FromStream(memoryStream, true, false))
                                    {
                                        graphics.DrawImage(imgtemp, x, y, cellWidth, cellHeight);
                                    }
                                }
                            }
                        }

                        index++;
                    }
                }
            }

            return img;
        }

        private const int SquareImageSize = 800;
        private const int ThumbImageWidth = 1600;
        private const int ThumbImageHeight = 900;

        private async Task<Image> GetSquareCollage(List<string> files)
        {
            if (files.Count < 4)
            {
                return await GetSingleImage(files).ConfigureAwait(false);
            }

            const int rows = 2;
            const int cols = 2;

            const int singleSize = SquareImageSize / 2;
            var index = 0;

            var img = new Bitmap(SquareImageSize, SquareImageSize, PixelFormat.Format32bppPArgb);

            using (var graphics = Graphics.FromImage(img))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceCopy;

                for (var row = 0; row < rows; row++)
                {
                    for (var col = 0; col < cols; col++)
                    {
                        var x = col * singleSize;
                        var y = row * singleSize;

                        using (var fileStream = _fileSystem.GetFileStream(files[index], FileMode.Open, FileAccess.Read, FileShare.Read, true))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                                memoryStream.Position = 0;

                                using (var imgtemp = Image.FromStream(memoryStream, true, false))
                                {
                                    graphics.DrawImage(imgtemp, x, y, singleSize, singleSize);
                                }
                            }
                        }

                        index++;
                    }
                }
            }

            return img;
        }

        private Task<Image> GetSingleImage(List<string> files)
        {
            return GetImage(files[0]);
        }

        private async Task<Image> GetImage(string file)
        {
            using (var fileStream = _fileSystem.GetFileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, true))
            {
                var memoryStream = new MemoryStream();

                await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                memoryStream.Position = 0;

                return Image.FromStream(memoryStream, true, false);
            }
        }

        public string Name
        {
            get { return "Dynamic Image Provider"; }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            var items = GetItemsWithImages(item);
            var cacheKey = GetConfigurationCacheKey(items);

            return HasChanged(item, ImageType.Primary, cacheKey) || HasChanged(item, ImageType.Thumb, cacheKey);
        }
    }
}
