using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Playlists
{
    public class PlaylistImageEnhancer : IImageEnhancer
    {
        private readonly IFileSystem _fileSystem;

        public PlaylistImageEnhancer(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public bool Supports(IHasImages item, ImageType imageType)
        {
            return imageType == ImageType.Primary && item is Playlist;
        }

        public MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        private List<BaseItem> GetItemsWithImages(IHasImages item)
        {
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

            // Rotate the images no more than once per day
            var random = new Random(DateTime.Now.DayOfYear).Next();

            return items
                .OrderBy(i => random - items.IndexOf(i))
                .Take(4)
                .OrderBy(i => i.Name)
                .ToList();
        }

        private const string Version = "3";

        public string GetConfigurationCacheKey(List<BaseItem> items)
        {
            return Version + "_" + string.Join(",", items.Select(i => i.Id.ToString("N")).ToArray());
        }

        public string GetConfigurationCacheKey(IHasImages item, ImageType imageType)
        {
            var items = GetItemsWithImages(item);

            return GetConfigurationCacheKey(items);
        }

        private const int ImageSize = 800;

        public ImageSize GetEnhancedImageSize(IHasImages item, ImageType imageType, int imageIndex, ImageSize originalImageSize)
        {
            var items = GetItemsWithImages(item);

            if (items.Count == 0)
            {
                return originalImageSize;
            }

            return new ImageSize
            {
                Height = ImageSize,
                Width = ImageSize
            };
        }

        public async Task<Image> EnhanceImageAsync(IHasImages item, Image originalImage, ImageType imageType, int imageIndex)
        {
            var items = GetItemsWithImages(item);

            if (items.Count == 0)
            {
                return originalImage;
            }

            var img = await GetCollage(items).ConfigureAwait(false);

            using (originalImage)
            {
                return img;
            }
        }

        private Task<Image> GetCollage(List<BaseItem> items)
        {
            return GetCollage(items.Select(i => i.GetImagePath(ImageType.Primary)).ToList());
        }

        private async Task<Image> GetCollage(List<string> files)
        {
            if (files.Count < 4)
            {
                return await GetSingleImage(files).ConfigureAwait(false);
            }

            const int rows = 2;
            const int cols = 2;

            const int singleSize = ImageSize / 2;
            var index = 0;

            var img = new Bitmap(ImageSize, ImageSize, PixelFormat.Format32bppPArgb);

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
    }
}
