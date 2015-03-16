using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.Photos;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.UserViews
{
    public class DynamicImageProvider : BaseDynamicImageProvider<UserView>, IPreRefreshProvider
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public DynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IUserManager userManager, ILibraryManager libraryManager)
            : base(fileSystem, providerManager, applicationPaths)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        public override IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            var view = (UserView)item;
            if (IsUsingCollectionStrip(view))
            {
                return new List<ImageType>
                {
                    ImageType.Primary
                };
            }

            return new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Thumb
            };
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (UserView)item;

            if (!view.UserId.HasValue)
            {
                return new List<BaseItem>();
            }

            if (string.Equals(view.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                return new List<BaseItem>();
            }

            if (string.Equals(view.ViewType, SpecialFolder.GameGenre, StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<BaseItem>();

                var genre = _libraryManager.GetGameGenre(view.Name);

                if (genre.HasImage(ImageType.Primary) || genre.HasImage(ImageType.Thumb))
                {
                    list.Add(genre);
                }
                return list;
            }
            if (string.Equals(view.ViewType, SpecialFolder.MusicGenre, StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<BaseItem>();

                var genre = _libraryManager.GetMusicGenre(view.Name);

                if (genre.HasImage(ImageType.Primary) || genre.HasImage(ImageType.Thumb))
                {
                    list.Add(genre);
                }
                return list;
            }
            if (string.Equals(view.ViewType, SpecialFolder.MovieGenre, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view.ViewType, SpecialFolder.TvGenre, StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<BaseItem>();

                var genre = _libraryManager.GetGenre(view.Name);

                if (genre.HasImage(ImageType.Primary) || genre.HasImage(ImageType.Thumb))
                {
                    list.Add(genre);
                }
                return list;
            }

            var isUsingCollectionStrip = IsUsingCollectionStrip(view);
            var recursive = isUsingCollectionStrip && !new[] { CollectionType.Playlists, CollectionType.Channels }.Contains(view.ViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = await view.GetItems(new InternalItemsQuery
            {
                User = _userManager.GetUserById(view.UserId.Value),
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder" }

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
                    var episodeSeason = episode.Season;
                    if (episodeSeason != null)
                    {
                        return episodeSeason;
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
                    var album = audio.FindParent<MusicAlbum>();
                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        return album;
                    }
                }

                return i;

            }).DistinctBy(i => i.Id);

            if (isUsingCollectionStrip)
            {
                return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb)).ToList(), 8);
            }

            return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary)).ToList());
        }

        public override bool Supports(IHasImages item)
        {
            var view = item as UserView;

            if (view != null && view.UserId.HasValue)
            {
                var supported = new[]
                {
                    SpecialFolder.TvFavoriteEpisodes,
                    SpecialFolder.TvFavoriteSeries,
                    SpecialFolder.TvGenres,
                    SpecialFolder.TvGenre,
                    SpecialFolder.TvLatest,
                    SpecialFolder.TvNextUp,
                    SpecialFolder.TvResume,
                    SpecialFolder.TvShowSeries,

                    SpecialFolder.MovieCollections,
                    SpecialFolder.MovieFavorites,
                    SpecialFolder.MovieGenres,
                    SpecialFolder.MovieGenre,
                    SpecialFolder.MovieLatest,
                    SpecialFolder.MovieMovies,
                    SpecialFolder.MovieResume,

                    SpecialFolder.GameFavorites,
                    SpecialFolder.GameGenres,
                    SpecialFolder.GameGenre,
                    SpecialFolder.GameSystems,
                    SpecialFolder.LatestGames,
                    SpecialFolder.RecentlyPlayedGames,

                    SpecialFolder.MusicArtists,
                    SpecialFolder.MusicAlbumArtists,
                    SpecialFolder.MusicAlbums,
                    SpecialFolder.MusicGenres,
                    SpecialFolder.MusicGenre,
                    SpecialFolder.MusicLatest,
                    SpecialFolder.MusicSongs,
                    SpecialFolder.MusicFavorites,
                    SpecialFolder.MusicFavoriteArtists,
                    SpecialFolder.MusicFavoriteAlbums,
                    SpecialFolder.MusicFavoriteSongs
                };

                return (IsUsingCollectionStrip(view) || supported.Contains(view.ViewType, StringComparer.OrdinalIgnoreCase)) &&
                    _userManager.GetUserById(view.UserId.Value) != null;
            }

            return false;
        }

        private bool IsUsingCollectionStrip(UserView view)
        {
            string[] collectionStripViewTypes =
            {
                CollectionType.Movies,
                CollectionType.TvShows,
                CollectionType.Games,
                CollectionType.Music,
                CollectionType.BoxSets,
                CollectionType.Playlists,
                CollectionType.Channels,
                CollectionType.LiveTv,
                CollectionType.Books,
                CollectionType.Photos
            };

            return collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }

        protected override Task<Stream> CreateImageAsync(IHasImages item, List<BaseItem> itemsWithImages, ImageType imageType, int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            var view = (UserView)item;
            if (imageType == ImageType.Primary && IsUsingCollectionStrip(view))
            {
                var stream = new StripCollageBuilder(ApplicationPaths).BuildThumbCollage(GetStripCollageImagePaths(itemsWithImages, view.ViewType), item.Name, 960, 540);
                return Task.FromResult(stream);
            }

            return base.CreateImageAsync(item, itemsWithImages, imageType, imageIndex);
        }

        private IEnumerable<String> GetStripCollageImagePaths(IEnumerable<BaseItem> items, string viewType)
        {
            if (string.Equals(viewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<string>();
                for (int i = 1; i <= 8; i++)
                {
                    list.Add(ExtractLiveTvResource(i.ToString(CultureInfo.InvariantCulture), ApplicationPaths));
                }
                return list;
            }

            return items
                .Select(i => i.GetImagePath(ImageType.Primary) ?? i.GetImagePath(ImageType.Thumb))
                .Where(i => !string.IsNullOrWhiteSpace(i));
        }

        private string ExtractLiveTvResource(string name, IApplicationPaths paths)
        {
            var namespacePath = GetType().Namespace + ".livetv." + name + ".jpg";
            var tempPath = Path.Combine(paths.TempDirectory, Guid.NewGuid().ToString("N") + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            using (var stream = GetType().Assembly.GetManifestResourceStream(namespacePath))
            {
                using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(fileStream);
                }
            }

            return tempPath;
        }
    }
}
