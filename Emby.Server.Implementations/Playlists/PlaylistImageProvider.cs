using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Images;

using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistImageProvider : BaseDynamicImageProvider<Playlist>
    {
        public PlaylistImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override List<BaseItem> GetItemsWithImages(IHasImages item)
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

                    var parent = subItem.GetParent();

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
    }

    public class MusicGenreImageProvider : BaseDynamicImageProvider<MusicGenre>
    {
        private readonly ILibraryManager _libraryManager;

        public MusicGenreImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override List<BaseItem> GetItemsWithImages(IHasImages item)
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                Genres = new[] { item.Name },
                IncludeItemTypes = new[] { typeof(MusicAlbum).Name, typeof(MusicVideo).Name, typeof(Audio).Name },
                SortBy = new[] { ItemSortBy.Random },
                Limit = 4,
                Recursive = true,
                ImageTypes = new[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)

            }).ToList();

            return GetFinalItems(items);
        }

        //protected override Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        //{
        //    return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        //}
    }

    public class GenreImageProvider : BaseDynamicImageProvider<Genre>
    {
        private readonly ILibraryManager _libraryManager;

        public GenreImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override List<BaseItem> GetItemsWithImages(IHasImages item)
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                Genres = new[] { item.Name },
                IncludeItemTypes = new[] { typeof(Series).Name, typeof(Movie).Name },
                SortBy = new[] { ItemSortBy.Random },
                Limit = 4,
                Recursive = true,
                ImageTypes = new[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)

            }).ToList();

            return GetFinalItems(items);
        }

        //protected override Task<string> CreateImage(IHasImages item, List<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        //{
        //    return CreateSingleImage(itemsWithImages, outputPathWithoutExtension, ImageType.Primary);
        //}
    }

}
