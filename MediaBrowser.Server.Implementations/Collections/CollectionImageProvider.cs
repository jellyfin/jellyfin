using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.Photos;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class CollectionImageProvider : BaseDynamicImageProvider<BoxSet>, ICustomMetadataProvider<BoxSet>
    {
        public CollectionImageProvider(IFileSystem fileSystem, IProviderManager providerManager)
            : base(fileSystem, providerManager)
        {
        }

        protected override Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var playlist = (BoxSet)item;

            var items = playlist.Children.Concat(playlist.GetLinkedChildren())
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

            return Task.FromResult(GetFinalItems(items));
        }
    }
}
