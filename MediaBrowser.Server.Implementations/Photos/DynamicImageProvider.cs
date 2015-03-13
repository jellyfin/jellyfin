using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Photos
{
    public class DynamicImageProvider : BaseDynamicImageProvider<UserView>
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public DynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IUserManager userManager, ILibraryManager libraryManager, string[] collectionStripViewTypes)
            : base(fileSystem, providerManager, applicationPaths)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _collectionStripViewTypes = collectionStripViewTypes;
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (UserView)item;

            if (!view.UserId.HasValue)
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

            var result = await view.GetItems(new InternalItemsQuery
            {
                User = _userManager.GetUserById(view.UserId.Value)

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
                    if (!audio.HasImage(ImageType.Primary))
                    {
                        var album = audio.FindParent<MusicAlbum>();
                        if (album != null)
                        {
                            return album;
                        }
                    }
                }

                return i;

            }).DistinctBy(i => i.Id);

            if (IsUsingCollectionStrip(view))
            {
                return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb)).ToList(), 8);
            }

            return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary)).ToList());
        }

        private readonly string[] _collectionStripViewTypes = { CollectionType.Movies };

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
            return _collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }
    }
}
