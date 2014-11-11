using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Server.Implementations.Photos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;

namespace MediaBrowser.Server.Implementations.Music
{
    public class MusicDynamicImageProvider : BaseDynamicImageProvider<UserView>, ICustomMetadataProvider<UserView>
    {
        private readonly IUserManager _userManager;

        public MusicDynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IUserManager userManager)
            : base(fileSystem, providerManager)
        {
            _userManager = userManager;
        }

        protected override async Task<List<BaseItem>> GetItemsWithImages(IHasImages item)
        {
            var view = (UserView)item;

            if (!view.UserId.HasValue)
            {
                return new List<BaseItem>();
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

                return i;

            }).DistinctBy(i => i.Id);

            return GetFinalItems(items.Where(i => i.HasImage(ImageType.Primary)).ToList());
        }

        protected override bool Supports(IHasImages item)
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

                return supported.Contains(view.ViewType, StringComparer.OrdinalIgnoreCase) &&
                    _userManager.GetUserById(view.UserId.Value) != null;
            }

            return false;
        }
    }
}
