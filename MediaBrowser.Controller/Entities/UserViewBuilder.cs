using System.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public class UserViewBuilder
    {
        private readonly IChannelManager _channelManager;
        private readonly ILiveTvManager _liveTvManager;
        private readonly IUserViewManager _userViewManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly ICollectionManager _collectionManager;

        public UserViewBuilder(IUserViewManager userViewManager, ILiveTvManager liveTvManager, IChannelManager channelManager, ILibraryManager libraryManager, ILogger logger, IUserDataManager userDataManager, ITVSeriesManager tvSeriesManager, ICollectionManager collectionManager)
        {
            _userViewManager = userViewManager;
            _liveTvManager = liveTvManager;
            _channelManager = channelManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _tvSeriesManager = tvSeriesManager;
            _collectionManager = collectionManager;
        }

        public async Task<QueryResult<BaseItem>> GetUserItems(Folder parent, string viewType, InternalItemsQuery query)
        {
            var user = query.User;

            switch (viewType)
            {
                case CollectionType.Channels:
                    {
                        var result = await _channelManager.GetChannelsInternal(new ChannelQuery
                        {
                            UserId = user.Id.ToString("N"),
                            Limit = query.Limit,
                            StartIndex = query.StartIndex

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case CollectionType.LiveTvChannels:
                    {
                        var result = await _liveTvManager.GetInternalChannels(new LiveTvChannelQuery
                        {
                            UserId = query.User.Id.ToString("N"),
                            Limit = query.Limit,
                            StartIndex = query.StartIndex

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case CollectionType.LiveTvNowPlaying:
                    {
                        var result = await _liveTvManager.GetRecommendedProgramsInternal(new RecommendedProgramQuery
                        {
                            UserId = query.User.Id.ToString("N"),
                            Limit = query.Limit,
                            IsAiring = true

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case CollectionType.LiveTvRecordingGroups:
                    {
                        var result = await _liveTvManager.GetInternalRecordings(new RecordingQuery
                        {
                            UserId = query.User.Id.ToString("N"),
                            Status = RecordingStatus.Completed,
                            Limit = query.Limit,
                            StartIndex = query.StartIndex

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case CollectionType.LiveTv:
                    {
                        var result = await GetLiveTvFolders(user).ConfigureAwait(false);

                        return GetResult(result, parent, query);
                    }

                case CollectionType.Folders:
                    return GetResult(user.RootFolder.GetChildren(user, true), parent, query);

                case CollectionType.Games:
                    return await GetGameView(user, parent, query).ConfigureAwait(false);

                case CollectionType.BoxSets:
                    return GetResult(GetMediaFolders(user).SelectMany(i => i.GetRecursiveChildren(user)).OfType<BoxSet>(), parent, query);

                case CollectionType.TvShows:
                    return await GetTvView(parent, user, query).ConfigureAwait(false);

                case CollectionType.Music:
                    return await GetMusicFolders(parent, user, query).ConfigureAwait(false);

                case CollectionType.Movies:
                    return await GetMovieFolders(parent, user, query).ConfigureAwait(false);

                case CollectionType.GameGenres:
                    return GetGameGenres(parent, user, query);

                case CollectionType.GameSystems:
                    return GetGameSystems(parent, user, query);

                case CollectionType.LatestGames:
                    return GetLatestGames(parent, user, query);

                case CollectionType.RecentlyPlayedGames:
                    return GetRecentlyPlayedGames(parent, user, query);

                case CollectionType.GameFavorites:
                    return GetFavoriteGames(parent, user, query);

                case CollectionType.TvShowSeries:
                    return GetTvSeries(parent, user, query);

                case CollectionType.TvGenres:
                    return GetTvGenres(parent, user, query);

                case CollectionType.TvResume:
                    return GetTvResume(parent, user, query);

                case CollectionType.TvNextUp:
                    return GetTvNextUp(parent, query);

                case CollectionType.TvLatest:
                    return GetTvLatest(parent, user, query);

                case CollectionType.MovieFavorites:
                    return GetFavoriteMovies(parent, user, query);

                case CollectionType.MovieLatest:
                    return GetMovieLatest(parent, user, query);

                case CollectionType.MovieGenres:
                    return GetMovieGenres(parent, user, query);

                case CollectionType.MovieResume:
                    return GetMovieResume(parent, user, query);

                case CollectionType.MovieMovies:
                    return GetMovieMovies(parent, user, query);

                case CollectionType.MovieCollections:
                    return GetMovieCollections(parent, user, query);

                case CollectionType.MusicLatest:
                    return GetMusicLatest(parent, user, query);

                case CollectionType.MusicAlbums:
                    return GetMusicAlbums(parent, user, query);

                case CollectionType.MusicAlbumArtists:
                    return GetMusicAlbumArtists(parent, user, query);

                case CollectionType.MusicArtists:
                    return GetMusicArtists(parent, user, query);

                case CollectionType.MusicSongs:
                    return GetMusicSongs(parent, user, query);

                case CollectionType.TvFavoriteEpisodes:
                    return GetFavoriteEpisodes(parent, user, query);

                case CollectionType.TvFavoriteSeries:
                    return GetFavoriteSeries(parent, user, query);

                case CollectionType.MusicFavorites:
                    return await GetMusicFavorites(parent, user, query).ConfigureAwait(false);

                case CollectionType.MusicFavoriteAlbums:
                    return GetFavoriteAlbums(parent, user, query);

                case CollectionType.MusicFavoriteArtists:
                    return GetFavoriteArtists(parent, user, query);

                case CollectionType.MusicFavoriteSongs:
                    return GetFavoriteSongs(parent, user, query);

                default:
                    return GetResult(GetMediaFolders(user).SelectMany(i => i.GetChildren(user, true)), parent, query);
            }
        }

        private int GetSpecialItemsLimit()
        {
            return 50;
        }

        private async Task<QueryResult<BaseItem>> GetMusicFolders(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos }), parent, query);
            }

            var list = new List<BaseItem>();

            var category = "music";

            list.Add(await GetUserView(category, CollectionType.MusicLatest, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicAlbums, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicAlbumArtists, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicSongs, user, "3", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MusicArtists, user, "3", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MusicGenres, user, "5", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicFavorites, user, "6", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetMusicFavorites(Folder parent, User user, InternalItemsQuery query)
        {
            var list = new List<BaseItem>();

            var category = "music";

            list.Add(await GetUserView(category, CollectionType.MusicFavoriteAlbums, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicFavoriteArtists, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicFavoriteSongs, user, "2", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetMusicAlbumArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var artists = GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos })
                .Where(i => !i.IsFolder)
                .OfType<IHasAlbumArtist>()
                .SelectMany(i => i.AlbumArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetArtist(i);
                    }
                    catch
                    {
                        // Already logged at lower levels
                        return null;
                    }
                })
                .Where(i => i != null);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetMusicArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var artists = GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos })
                .Where(i => !i.IsFolder)
                .OfType<IHasArtist>()
                .SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetArtist(i);
                    }
                    catch
                    {
                        // Already logged at lower levels
                        return null;
                    }
                })
                .Where(i => i != null);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var artists = GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos })
                .Where(i => !i.IsFolder)
                .OfType<IHasAlbumArtist>()
                .SelectMany(i => i.AlbumArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetArtist(i);
                    }
                    catch
                    {
                        // Already logged at lower levels
                        return null;
                    }
                })
                .Where(i => i != null && _userDataManager.GetUserData(user.Id, i.GetUserDataKey()).IsFavorite);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetMusicAlbums(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos }).Where(i => i is MusicAlbum), parent, query);
        }

        private QueryResult<BaseItem> GetMusicSongs(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos }).Where(i => i is Audio.Audio), parent, query);
        }

        private QueryResult<BaseItem> GetMusicLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos }).Where(i => i is MusicVideo || i is Audio.Audio), parent, GetSpecialItemsLimit(), query);
        }

        private async Task<QueryResult<BaseItem>> GetMovieFolders(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie || i is BoxSet), parent, query);
            }

            var list = new List<BaseItem>();

            var category = "movies";

            list.Add(await GetUserView(category, CollectionType.MovieResume, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieLatest, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieMovies, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieCollections, user, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieFavorites, user, "4", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MovieGenres, user, "5", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Series), parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteEpisodes(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Episode), parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteSongs(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music }).Where(i => i is Audio.Audio), parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteAlbums(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music }).Where(i => i is MusicAlbum), parent, query);
        }

        private QueryResult<BaseItem> GetMovieMovies(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), parent, query);
        }

        private QueryResult<BaseItem> GetMovieCollections(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is BoxSet), parent, query);
        }

        private QueryResult<BaseItem> GetMovieLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetMovieResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;
            query.IsResumable = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetMovieGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty })
                .Where(i => i is Movie)
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetGenre(i);
                    }
                    catch
                    {
                        // Full exception logged at lower levels
                        _logger.Error("Error getting genre");
                        return null;
                    }

                })
                .Where(i => i != null);

            return GetResult(genres, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetTvView(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Series || i is Season || i is Episode), parent, query);
            }

            var list = new List<BaseItem>();

            var category = "tvshows";

            list.Add(await GetUserView(category, CollectionType.TvResume, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvNextUp, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvLatest, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvShowSeries, user, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvFavoriteSeries, user, "4", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvFavoriteEpisodes, user, "5", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.TvGenres, user, "5", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetGameView(User user, Folder parent, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }), parent, query);
            }

            var list = new List<BaseItem>();

            var category = "games";

            list.Add(await GetUserView(category, CollectionType.LatestGames, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.RecentlyPlayedGames, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.GameFavorites, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.GameSystems, user, "3", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.GameGenres, user, "4", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetLatestGames(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetRecentlyPlayedGames(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsPlayed = true;
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetFavoriteGames(Folder parent, User user, InternalItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), parent, query);
        }

        private QueryResult<BaseItem> GetTvLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Episode>(), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetTvNextUp(Folder parent, InternalItemsQuery query)
        {
            var parentFolders = GetMediaFolders(parent, query.User, new[] { CollectionType.TvShows, string.Empty });

            var result = _tvSeriesManager.GetNextUp(new NextUpQuery
            {
                Limit = query.Limit,
                StartIndex = query.StartIndex,
                UserId = query.User.Id.ToString("N")

            }, parentFolders);

            return result;
        }

        private QueryResult<BaseItem> GetTvResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;
            query.IsResumable = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Episode>(), parent, GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetTvSeries(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Series>(), parent, query);
        }

        private QueryResult<BaseItem> GetTvGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty })
                .OfType<Series>()
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetGenre(i);
                    }
                    catch
                    {
                        // Full exception logged at lower levels
                        _logger.Error("Error getting genre");
                        return null;
                    }

                })
                .Where(i => i != null);

            return GetResult(genres, parent, query);
        }

        private QueryResult<BaseItem> GetGameSystems(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<GameSystem>(), parent, query);
        }

        private QueryResult<BaseItem> GetGameGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = GetRecursiveChildren(parent, user, new[] { CollectionType.Games })
                .OfType<Game>()
                .SelectMany(i => i.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetGameGenre(i);
                    }
                    catch
                    {
                        // Full exception logged at lower levels
                        _logger.Error("Error getting game genre");
                        return null;
                    }

                })
                .Where(i => i != null);

            return GetResult(genres, parent, query);
        }

        private QueryResult<BaseItem> GetResult<T>(QueryResult<T> result)
            where T : BaseItem
        {
            return new QueryResult<BaseItem>
            {
                Items = result.Items,
                TotalRecordCount = result.TotalRecordCount
            };
        }

        private QueryResult<BaseItem> GetResult<T>(IEnumerable<T> items,
            BaseItem parentItem,
            InternalItemsQuery query)
            where T : BaseItem
        {
            return GetResult(items, parentItem, null, query);
        }

        private QueryResult<BaseItem> GetResult<T>(IEnumerable<T> items,
            BaseItem parentItem,
            int? totalRecordLimit,
            InternalItemsQuery query)
            where T : BaseItem
        {
            return SortAndFilter(items, parentItem, totalRecordLimit, query, _libraryManager, _userDataManager);
        }

        public static QueryResult<BaseItem> SortAndFilter(IEnumerable<BaseItem> items,
            BaseItem parentItem,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager,
            IUserDataManager userDataManager)
        {
            var user = query.User;

            items = items.Where(i => Filter(i, user, query, userDataManager));

            items = FilterVirtualEpisodes(items,
                query.IsMissing,
                query.IsVirtualUnaired,
                query.IsUnaired);

            items = CollapseBoxSetItemsIfNeeded(items, query, parentItem, user);

            // This must be the last filter
            if (!string.IsNullOrEmpty(query.AdjacentTo))
            {
                items = FilterForAdjacency(items, query.AdjacentTo);
            }

            return Sort(items, totalRecordLimit, query, libraryManager);
        }

        public static IEnumerable<BaseItem> CollapseBoxSetItemsIfNeeded(IEnumerable<BaseItem> items,
            InternalItemsQuery query,
            BaseItem parentItem,
            User user)
        {
            if (CollapseBoxSetItems(query, parentItem, user))
            {
                items = BaseItem.CollectionManager.CollapseItemsWithinBoxSets(items, user);
            }

            items = ApplyPostCollectionCollapseFilters(query, items, user);

            return items;
        }

        private static IEnumerable<BaseItem> ApplyPostCollectionCollapseFilters(InternalItemsQuery request,
            IEnumerable<BaseItem> items,
            User user)
        {
            if (!string.IsNullOrEmpty(request.NameStartsWithOrGreater))
            {
                items = items.Where(i => string.Compare(request.NameStartsWithOrGreater, i.SortName, StringComparison.CurrentCultureIgnoreCase) < 1);
            }
            if (!string.IsNullOrEmpty(request.NameStartsWith))
            {
                items = items.Where(i => string.Compare(request.NameStartsWith, i.SortName.Substring(0, 1), StringComparison.CurrentCultureIgnoreCase) == 0);
            }

            if (!string.IsNullOrEmpty(request.NameLessThan))
            {
                items = items.Where(i => string.Compare(request.NameLessThan, i.SortName, StringComparison.CurrentCultureIgnoreCase) == 1);
            }

            return items;
        }

        private static bool CollapseBoxSetItems(InternalItemsQuery query,
            BaseItem parentItem,
            User user)
        {
            // Could end up stuck in a loop like this
            if (parentItem is BoxSet)
            {
                return false;
            }

            var param = query.CollapseBoxSetItems;

            if (!param.HasValue)
            {
                if (user != null && !user.Configuration.GroupMoviesIntoBoxSets)
                {
                    return false;
                }

                if (query.IncludeItemTypes.Contains("Movie", StringComparer.OrdinalIgnoreCase))
                {
                    param = true;
                }
            }

            return param.HasValue && param.Value && AllowBoxSetCollapsing(query);
        }

        private static bool AllowBoxSetCollapsing(InternalItemsQuery request)
        {
            if (request.IsFavorite.HasValue)
            {
                return false;
            }
            if (request.IsFavoriteOrLiked.HasValue)
            {
                return false;
            }
            if (request.IsLiked.HasValue)
            {
                return false;
            }
            if (request.IsPlayed.HasValue)
            {
                return false;
            }
            if (request.IsResumable.HasValue)
            {
                return false;
            }
            if (request.IsFolder.HasValue)
            {
                return false;
            }

            if (request.AllGenres.Length > 0)
            {
                return false;
            }

            if (request.Genres.Length > 0)
            {
                return false;
            }

            if (request.HasImdbId.HasValue)
            {
                return false;
            }

            if (request.HasOfficialRating.HasValue)
            {
                return false;
            }

            if (request.HasOverview.HasValue)
            {
                return false;
            }

            if (request.HasParentalRating.HasValue)
            {
                return false;
            }

            if (request.HasSpecialFeature.HasValue)
            {
                return false;
            }

            if (request.HasSubtitles.HasValue)
            {
                return false;
            }

            if (request.HasThemeSong.HasValue)
            {
                return false;
            }

            if (request.HasThemeVideo.HasValue)
            {
                return false;
            }

            if (request.HasTmdbId.HasValue)
            {
                return false;
            }

            if (request.HasTrailer.HasValue)
            {
                return false;
            }

            if (request.ImageTypes.Length > 0)
            {
                return false;
            }

            if (request.Is3D.HasValue)
            {
                return false;
            }

            if (request.IsHD.HasValue)
            {
                return false;
            }

            if (request.IsInBoxSet.HasValue)
            {
                return false;
            }

            if (request.IsLocked.HasValue)
            {
                return false;
            }

            if (request.IsPlaceHolder.HasValue)
            {
                return false;
            }

            if (request.IsPlayed.HasValue)
            {
                return false;
            }

            if (request.IsUnidentified.HasValue)
            {
                return false;
            }

            if (request.IsYearMismatched.HasValue)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(request.Person))
            {
                return false;
            }

            if (request.Studios.Length > 0)
            {
                return false;
            }

            if (request.VideoTypes.Length > 0)
            {
                return false;
            }

            if (request.Years.Length > 0)
            {
                return false;
            }

            return true;
        }

        public static IEnumerable<BaseItem> FilterVirtualEpisodes(
            IEnumerable<BaseItem> items,
            bool? isMissing,
            bool? isVirtualUnaired,
            bool? isUnaired)
        {
            items = FilterVirtualSeasons(items, isMissing, isVirtualUnaired, isUnaired);

            if (isMissing.HasValue)
            {
                var val = isMissing.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsMissingEpisode == val;
                    }
                    return true;
                });
            }

            if (isUnaired.HasValue)
            {
                var val = isUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsUnaired == val;
                    }
                    return true;
                });
            }

            if (isVirtualUnaired.HasValue)
            {
                var val = isVirtualUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Episode;
                    if (e != null)
                    {
                        return e.IsVirtualUnaired == val;
                    }
                    return true;
                });
            }

            return items;
        }

        private static IEnumerable<BaseItem> FilterVirtualSeasons(
            IEnumerable<BaseItem> items,
            bool? isMissing,
            bool? isVirtualUnaired,
            bool? isUnaired)
        {
            if (isMissing.HasValue && isVirtualUnaired.HasValue)
            {
                if (!isMissing.Value && !isVirtualUnaired.Value)
                {
                    return items.Where(i =>
                    {
                        var e = i as Season;
                        if (e != null)
                        {
                            return !e.IsMissingOrVirtualUnaired;
                        }
                        return true;
                    });
                }
            }

            if (isMissing.HasValue)
            {
                var val = isMissing.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsMissingSeason == val;
                    }
                    return true;
                });
            }

            if (isUnaired.HasValue)
            {
                var val = isUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsUnaired == val;
                    }
                    return true;
                });
            }

            if (isVirtualUnaired.HasValue)
            {
                var val = isVirtualUnaired.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return e.IsVirtualUnaired == val;
                    }
                    return true;
                });
            }

            return items;
        }

        public static QueryResult<BaseItem> Sort(IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager)
        {
            var user = query.User;

            items = libraryManager.ReplaceVideosWithPrimaryVersions(items);

            if (query.SortBy.Length > 0)
            {
                items = libraryManager.Sort(items, user, query.SortBy, query.SortOrder);
            }

            var itemsArray = totalRecordLimit.HasValue ? items.Take(totalRecordLimit.Value).ToArray() : items.ToArray();
            var totalCount = itemsArray.Length;

            if (query.Limit.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex ?? 0).Take(query.Limit.Value).ToArray();
            }
            else if (query.StartIndex.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex.Value).ToArray();
            }

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = itemsArray
            };
        }

        private static bool Filter(BaseItem item, User user, InternalItemsQuery query, IUserDataManager userDataManager)
        {
            if (query.MediaTypes.Length > 0 && !query.MediaTypes.Contains(item.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length > 0 && !query.IncludeItemTypes.Contains(item.GetClientTypeName(), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.ExcludeItemTypes.Length > 0 && query.ExcludeItemTypes.Contains(item.GetClientTypeName(), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IsFolder.HasValue && query.IsFolder.Value != item.IsFolder)
            {
                return false;
            }

            if (query.Filter != null && !query.Filter(item, user))
            {
                return false;
            }

            UserItemData userData = null;

            if (query.IsLiked.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user.Id, item.GetUserDataKey());

                if (!userData.Likes.HasValue || userData.Likes != query.IsLiked.Value)
                {
                    return false;
                }
            }

            if (query.IsFavoriteOrLiked.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user.Id, item.GetUserDataKey());
                var isFavoriteOrLiked = userData.IsFavorite || (userData.Likes ?? false);

                if (isFavoriteOrLiked != query.IsFavoriteOrLiked.Value)
                {
                    return false;
                }
            }

            if (query.IsFavorite.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user.Id, item.GetUserDataKey());

                if (userData.IsFavorite != query.IsFavorite.Value)
                {
                    return false;
                }
            }

            if (query.IsResumable.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user.Id, item.GetUserDataKey());
                var isResumable = userData.PlaybackPositionTicks > 0;

                if (isResumable != query.IsResumable.Value)
                {
                    return false;
                }
            }

            if (query.IsPlayed.HasValue)
            {
                if (item.IsPlayed(user) != query.IsPlayed.Value)
                {
                    return false;
                }
            }

            if (query.IsInBoxSet.HasValue)
            {
                var val = query.IsInBoxSet.Value;
                if (item.Parents.OfType<BoxSet>().Any() != val)
                {
                    return false;
                }
            }

            // Filter by Video3DFormat
            if (query.Is3D.HasValue)
            {
                var val = query.Is3D.Value;
                var video = item as Video;

                if (video == null || val != video.Video3DFormat.HasValue)
                {
                    return false;
                }
            }

            if (query.IsHD.HasValue)
            {
                var val = query.IsHD.Value;
                var video = item as Video;

                if (video == null || val != video.IsHD)
                {
                    return false;
                }
            }

            if (query.IsUnidentified.HasValue)
            {
                var val = query.IsUnidentified.Value;
                if (item.IsUnidentified != val)
                {
                    return false;
                }
            }

            if (query.IsLocked.HasValue)
            {
                var val = query.IsLocked.Value;
                if (item.IsLocked != val)
                {
                    return false;
                }
            }

            if (query.HasOverview.HasValue)
            {
                var filterValue = query.HasOverview.Value;

                var hasValue = !string.IsNullOrEmpty(item.Overview);

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.HasImdbId.HasValue)
            {
                var filterValue = query.HasImdbId.Value;

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Imdb));

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.HasTmdbId.HasValue)
            {
                var filterValue = query.HasTmdbId.Value;

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tmdb));

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.HasTvdbId.HasValue)
            {
                var filterValue = query.HasTvdbId.Value;

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tvdb));

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.IsYearMismatched.HasValue)
            {
                var filterValue = query.IsYearMismatched.Value;

                if (IsYearMismatched(item) != filterValue)
                {
                    return false;
                }
            }

            if (query.HasOfficialRating.HasValue)
            {
                var filterValue = query.HasOfficialRating.Value;

                var hasValue = !string.IsNullOrEmpty(item.OfficialRating);

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.IsPlaceHolder.HasValue)
            {
                var filterValue = query.IsPlaceHolder.Value;

                var isPlaceHolder = false;

                var hasPlaceHolder = item as ISupportsPlaceHolders;

                if (hasPlaceHolder != null)
                {
                    isPlaceHolder = hasPlaceHolder.IsPlaceHolder;
                }

                if (isPlaceHolder != filterValue)
                {
                    return false;
                }
            }

            if (query.HasSpecialFeature.HasValue)
            {
                var filterValue = query.HasSpecialFeature.Value;

                var movie = item as IHasSpecialFeatures;

                if (movie != null)
                {
                    var ok = filterValue
                        ? movie.SpecialFeatureIds.Count > 0
                        : movie.SpecialFeatureIds.Count == 0;

                    if (!ok)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (query.HasSubtitles.HasValue)
            {
                var val = query.HasSubtitles.Value;

                var video = item as Video;

                if (video == null || val != video.HasSubtitles)
                {
                    return false;
                }
            }

            if (query.HasParentalRating.HasValue)
            {
                var val = query.HasParentalRating.Value;

                var rating = item.CustomRating;

                if (string.IsNullOrEmpty(rating))
                {
                    rating = item.OfficialRating;
                }

                if (val)
                {
                    if (string.IsNullOrEmpty(rating))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(rating))
                    {
                        return false;
                    }
                }
            }

            if (query.HasTrailer.HasValue)
            {
                var val = query.HasTrailer.Value;
                var trailerCount = 0;

                var hasTrailers = item as IHasTrailers;
                if (hasTrailers != null)
                {
                    trailerCount = hasTrailers.LocalTrailerIds.Count;
                }

                var ok = val ? trailerCount > 0 : trailerCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            if (query.HasThemeSong.HasValue)
            {
                var filterValue = query.HasThemeSong.Value;

                var themeCount = 0;
                var iHasThemeMedia = item as IHasThemeMedia;

                if (iHasThemeMedia != null)
                {
                    themeCount = iHasThemeMedia.ThemeSongIds.Count;
                }
                var ok = filterValue ? themeCount > 0 : themeCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            if (query.HasThemeVideo.HasValue)
            {
                var filterValue = query.HasThemeVideo.Value;

                var themeCount = 0;
                var iHasThemeMedia = item as IHasThemeMedia;

                if (iHasThemeMedia != null)
                {
                    themeCount = iHasThemeMedia.ThemeVideoIds.Count;
                }
                var ok = filterValue ? themeCount > 0 : themeCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            // Apply genre filter
            if (query.Genres.Length > 0 && !(query.Genres.Any(v => item.Genres.Contains(v, StringComparer.OrdinalIgnoreCase))))
            {
                return false;
            }

            // Apply genre filter
            if (query.AllGenres.Length > 0 && !query.AllGenres.All(v => item.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Filter by VideoType
            if (query.VideoTypes.Length > 0)
            {
                var video = item as Video;
                if (video == null || !query.VideoTypes.Contains(video.VideoType))
                {
                    return false;
                }
            }

            if (query.ImageTypes.Length > 0 && !query.ImageTypes.Any(item.HasImage))
            {
                return false;
            }

            // Apply studio filter
            if (query.Studios.Length > 0 && !query.Studios.Any(v => item.Studios.Contains(v, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Apply year filter
            if (query.Years.Length > 0)
            {
                if (!(item.ProductionYear.HasValue && query.Years.Contains(item.ProductionYear.Value)))
                {
                    return false;
                }
            }

            // Apply person filter
            if (!string.IsNullOrEmpty(query.Person))
            {
                var personTypes = query.PersonTypes;

                if (personTypes.Length == 0)
                {
                    if (!(item.People.Any(p => string.Equals(p.Name, query.Person, StringComparison.OrdinalIgnoreCase))))
                    {
                        return false;
                    }
                }
                else
                {
                    var types = personTypes;

                    var ok = new[] { item }.Any(i =>
                            i.People != null &&
                            i.People.Any(p =>
                                p.Name.Equals(query.Person, StringComparison.OrdinalIgnoreCase) && (types.Contains(p.Type, StringComparer.OrdinalIgnoreCase) || types.Contains(p.Role, StringComparer.OrdinalIgnoreCase))));

                    if (!ok)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private IEnumerable<Folder> GetMediaFolders(User user)
        {
            var excludeFolderIds = user.Configuration.ExcludeFoldersFromGrouping.Select(i => new Guid(i)).ToList();

            return user.RootFolder
                .GetChildren(user, true, true)
                .OfType<Folder>()
                .Where(i => !excludeFolderIds.Contains(i.Id) && !UserView.IsExcludedFromGrouping(i));
        }

        private IEnumerable<Folder> GetMediaFolders(User user, IEnumerable<string> viewTypes)
        {
            return GetMediaFolders(user)
                .Where(i =>
                {
                    var folder = i as ICollectionFolder;

                    return folder != null && viewTypes.Contains(folder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                });
        }

        private IEnumerable<Folder> GetMediaFolders(Folder parent, User user, string[] viewTypes)
        {
            if (parent == null || parent is UserView)
            {
                return GetMediaFolders(user, viewTypes);
            }

            return new[] { parent };
        }

        private IEnumerable<BaseItem> GetRecursiveChildren(Folder parent, User user, string[] viewTypes)
        {
            if (parent == null || parent is UserView)
            {
                return GetMediaFolders(user, viewTypes).SelectMany(i => i.GetRecursiveChildren(user));
            }

            return parent.GetRecursiveChildren(user);
        }

        private async Task<IEnumerable<BaseItem>> GetLiveTvFolders(User user)
        {
            var list = new List<BaseItem>();

            list.Add(await _userViewManager.GetUserView("livetv", CollectionType.LiveTvNowPlaying, user, "0", CancellationToken.None).ConfigureAwait(false));
            list.Add(await _userViewManager.GetUserView("livetv", CollectionType.LiveTvChannels, user, string.Empty, CancellationToken.None).ConfigureAwait(false));
            list.Add(await _userViewManager.GetUserView("livetv", CollectionType.LiveTvRecordingGroups, user, string.Empty, CancellationToken.None).ConfigureAwait(false));

            return list;
        }

        private async Task<UserView> GetUserView(string category, string type, User user, string sortName, Folder parent)
        {
            var view = await _userViewManager.GetUserView(category, type, user, sortName, CancellationToken.None)
                        .ConfigureAwait(false);

            if (parent.Id != view.ParentId)
            {
                view.ParentId = parent.Id;
                await view.UpdateToRepository(ItemUpdateType.MetadataImport, CancellationToken.None)
                        .ConfigureAwait(false);
            }

            return view;
        }

        public static bool IsYearMismatched(BaseItem item)
        {
            if (item.ProductionYear.HasValue)
            {
                var path = item.Path;

                if (!string.IsNullOrEmpty(path))
                {
                    int? yearInName;
                    string name;
                    NameParser.ParseName(Path.GetFileName(path), out name, out yearInName);

                    // Go up a level if we didn't get a year
                    if (!yearInName.HasValue)
                    {
                        NameParser.ParseName(Path.GetFileName(Path.GetDirectoryName(path)), out name, out yearInName);
                    }

                    if (yearInName.HasValue)
                    {
                        return yearInName.Value != item.ProductionYear.Value;
                    }
                }
            }

            return false;
        }

        public static IEnumerable<BaseItem> FilterForAdjacency(IEnumerable<BaseItem> items, string adjacentToId)
        {
            var list = items.ToList();

            var adjacentToIdGuid = new Guid(adjacentToId);
            var adjacentToItem = list.FirstOrDefault(i => i.Id == adjacentToIdGuid);

            var index = list.IndexOf(adjacentToItem);

            var previousId = Guid.Empty;
            var nextId = Guid.Empty;

            if (index > 0)
            {
                previousId = list[index - 1].Id;
            }

            if (index < list.Count - 1)
            {
                nextId = list[index + 1].Id;
            }

            return list.Where(i => i.Id == previousId || i.Id == nextId || i.Id == adjacentToIdGuid);
        }
    }
}
