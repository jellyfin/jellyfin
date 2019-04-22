using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Server.Implementations.Images;
using Jellyfin.Common.Configuration;
using Jellyfin.Controller.Drawing;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Entities.TV;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;

namespace Jellyfin.Server.Implementations.Collections
{
    public class CollectionImageProvider : BaseDynamicImageProvider<BoxSet>
    {
        public CollectionImageProvider(
            IFileSystem fileSystem,
            IProviderManager providerManager,
            IApplicationPaths applicationPaths,
            IImageProcessor imageProcessor)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override bool Supports(BaseItem item)
        {
            // Right now this is the only way to prevent this image from getting created ahead of internet image providers
            if (!item.IsLocked)
            {
                return false;
            }

            return base.Supports(item);
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var playlist = (BoxSet)item;

            return playlist.Children.Concat(playlist.GetLinkedChildren())
                .Select(i =>
                {
                    var subItem = i;

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

                    var parent = subItem.GetOwner() ?? subItem.GetParent();

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
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .OrderBy(i => Guid.NewGuid())
                .ToList();
        }

        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        }
    }
}
