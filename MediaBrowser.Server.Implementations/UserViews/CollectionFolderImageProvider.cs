using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.Photos;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.UserViews
{
    public class CollectionFolderImageProvider : BaseDynamicImageProvider<CollectionFolder>
    {
        public CollectionFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        public override IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
                {
                    ImageType.Primary
                };
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (CollectionFolder)item;

            var recursive = !new[] { CollectionType.Playlists, CollectionType.Channels }.Contains(view.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = await view.GetItems(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder", "Playlist" }

            }).ConfigureAwait(false);

            var items = result.Items.Select(i =>
            {
                var episode = i as Episode;
                if (episode != null)
                {
                    var series = episode.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return episode;
                }

                var season = i as Season;
                if (season != null)
                {
                    var series = season.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return season;
                }

                var audio = i as Audio;
                if (audio != null)
                {
                    var album = audio.AlbumEntity;
                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        return album;
                    }
                }

                return i;

            }).DistinctBy(i => i.Id);

            return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb)).ToList(), 8);
        }

        protected override bool Supports(IHasImages item)
        {
            return item is CollectionFolder;
        }

        protected override async Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            if (imageType == ImageType.Primary)
            {
                if (itemsWithImages.Count == 0)
                {
                    return null;
                }

                return await CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540).ConfigureAwait(false);
            }

            return await base.CreateImage(item, itemsWithImages, outputPath, imageType, imageIndex).ConfigureAwait(false);
        }
    }
}
