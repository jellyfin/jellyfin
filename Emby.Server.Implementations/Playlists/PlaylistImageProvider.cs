using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistImageProvider : BaseDynamicImageProvider<Playlist>
    {
        public PlaylistImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var playlist = (Playlist)item;

            return playlist.GetManageableItems()
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
                .ToList();
        }
    }

    public class MusicGenreImageProvider : BaseDynamicImageProvider<MusicGenre>
    {
        private readonly ILibraryManager _libraryManager;

        public MusicGenreImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Genres = new[] { item.Name },
                IncludeItemTypes = new[] { typeof(MusicAlbum).Name, typeof(MusicVideo).Name, typeof(Audio).Name },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = 4,
                Recursive = true,
                ImageTypes = new[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)
            });
        }
    }

    public class GenreImageProvider : BaseDynamicImageProvider<Genre>
    {
        private readonly ILibraryManager _libraryManager;

        public GenreImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                Genres = new[] { item.Name },
                IncludeItemTypes = new[] { typeof(Series).Name, typeof(Movie).Name },
                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },
                Limit = 4,
                Recursive = true,
                ImageTypes = new[] { ImageType.Primary },
                DtoOptions = new DtoOptions(false)
            });
        }
    }
}
