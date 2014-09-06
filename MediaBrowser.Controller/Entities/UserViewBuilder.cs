using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
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

        public UserViewBuilder(IUserViewManager userViewManager, ILiveTvManager liveTvManager, IChannelManager channelManager, ILibraryManager libraryManager, ILogger logger, IUserDataManager userDataManager, ITVSeriesManager tvSeriesManager)
        {
            _userViewManager = userViewManager;
            _liveTvManager = liveTvManager;
            _channelManager = channelManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _tvSeriesManager = tvSeriesManager;
        }

        public async Task<QueryResult<BaseItem>> GetUserItems(Folder parent, string viewType, UserItemsQuery query)
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

                        return GetResult(result, query);
                    }

                case CollectionType.Folders:
                    return GetResult(user.RootFolder.GetChildren(user, true), query);

                case CollectionType.Games:
                    return await GetGameView(user, parent, query).ConfigureAwait(false);

                case CollectionType.BoxSets:
                    return GetResult(GetMediaFolders(user).SelectMany(i => i.GetRecursiveChildren(user)).OfType<BoxSet>(), query);

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

                case CollectionType.ViewTypeTvShowSeries:
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

                case CollectionType.TvFavoriteEpisodes:
                    return GetFavoriteEpisodes(parent, user, query);

                case CollectionType.TvFavoriteSeries:
                    return GetFavoriteSeries(parent, user, query);

                default:
                    return GetResult(GetMediaFolders(user).SelectMany(i => i.GetChildren(user, true)), query);
            }
        }

        private int GetSpecialItemsLimit()
        {
            return 50;
        }

        private async Task<QueryResult<BaseItem>> GetMusicFolders(Folder parent, User user, UserItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music }), query);
            }

            var list = new List<BaseItem>();

            var category = "music";

            list.Add(await GetUserView(category, CollectionType.MusicLatest, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicAlbums, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MusicAlbumArtists, user, "2", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MusicArtists, user, "3", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MusicGenres, user, "5", parent).ConfigureAwait(false));

            return GetResult(list, query);
        }

        private QueryResult<BaseItem> GetMusicAlbumArtists(Folder parent, User user, UserItemsQuery query)
        {
            var artists = GetRecursiveChildren(parent, user, new[] { CollectionType.Music })
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

            return GetResult(artists, query);
        }

        private QueryResult<BaseItem> GetMusicArtists(Folder parent, User user, UserItemsQuery query)
        {
            var artists = GetRecursiveChildren(parent, user, new[] { CollectionType.Music })
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

            return GetResult(artists, query);
        }

        private QueryResult<BaseItem> GetMusicAlbums(Folder parent, User user, UserItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music }).Where(i => i is MusicAlbum), query);
        }

        private QueryResult<BaseItem> GetMusicLatest(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Music }).Where(i => i is MusicVideo || i is Audio.Audio), GetSpecialItemsLimit(), query);
        }

        private async Task<QueryResult<BaseItem>> GetMovieFolders(Folder parent, User user, UserItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie || i is BoxSet), query);
            }

            var list = new List<BaseItem>();

            var category = "movies";

            list.Add(await GetUserView(category, CollectionType.MovieResume, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieLatest, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieMovies, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieCollections, user, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.MovieFavorites, user, "4", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.MovieGenres, user, "5", parent).ConfigureAwait(false));

            return GetResult(list, query);
        }

        private QueryResult<BaseItem> GetFavoriteMovies(Folder parent, User user, UserItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), query);
        }

        private QueryResult<BaseItem> GetFavoriteSeries(Folder parent, User user, UserItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Series), query);
        }

        private QueryResult<BaseItem> GetFavoriteEpisodes(Folder parent, User user, UserItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Episode), query);
        }

        private QueryResult<BaseItem> GetMovieMovies(Folder parent, User user, UserItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), query);
        }

        private QueryResult<BaseItem> GetMovieCollections(Folder parent, User user, UserItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is BoxSet), query);
        }

        private QueryResult<BaseItem> GetMovieLatest(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetMovieResume(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;
            query.IsResumable = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Movies, CollectionType.BoxSets, string.Empty }).Where(i => i is Movie), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetMovieGenres(Folder parent, User user, UserItemsQuery query)
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

            return GetResult(genres, query);
        }

        private async Task<QueryResult<BaseItem>> GetTvView(Folder parent, User user, UserItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).Where(i => i is Series || i is Season || i is Episode), query);
            }

            var list = new List<BaseItem>();

            var category = "tv";

            list.Add(await GetUserView(category, CollectionType.TvResume, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvNextUp, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvLatest, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.ViewTypeTvShowSeries, user, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvFavoriteSeries, user, "4", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.TvFavoriteEpisodes, user, "5", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.TvGenres, user, "5", parent).ConfigureAwait(false));

            return GetResult(list, query);
        }

        private async Task<QueryResult<BaseItem>> GetGameView(User user, Folder parent, UserItemsQuery query)
        {
            if (query.Recursive)
            {
                return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }), query);
            }

            var list = new List<BaseItem>();

            var category = "games";

            list.Add(await GetUserView(category, CollectionType.LatestGames, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.RecentlyPlayedGames, user, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.GameFavorites, user, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(category, CollectionType.GameSystems, user, "3", parent).ConfigureAwait(false));
            //list.Add(await GetUserView(CollectionType.GameGenres, user, "4", parent).ConfigureAwait(false));

            return GetResult(list, query);
        }

        private QueryResult<BaseItem> GetLatestGames(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetRecentlyPlayedGames(Folder parent, User user, UserItemsQuery query)
        {
            query.IsPlayed = true;
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetFavoriteGames(Folder parent, User user, UserItemsQuery query)
        {
            query.IsFavorite = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<Game>(), query);
        }

        private QueryResult<BaseItem> GetTvLatest(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Episode>(), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetTvNextUp(Folder parent, UserItemsQuery query)
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

        private QueryResult<BaseItem> GetTvResume(Folder parent, User user, UserItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;
            query.IsResumable = true;

            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Episode>(), GetSpecialItemsLimit(), query);
        }

        private QueryResult<BaseItem> GetTvSeries(Folder parent, User user, UserItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.TvShows, string.Empty }).OfType<Series>(), query);
        }

        private QueryResult<BaseItem> GetTvGenres(Folder parent, User user, UserItemsQuery query)
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

            return GetResult(genres, query);
        }

        private QueryResult<BaseItem> GetGameSystems(Folder parent, User user, UserItemsQuery query)
        {
            return GetResult(GetRecursiveChildren(parent, user, new[] { CollectionType.Games }).OfType<GameSystem>(), query);
        }

        private QueryResult<BaseItem> GetGameGenres(Folder parent, User user, UserItemsQuery query)
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

            return GetResult(genres, query);
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
            UserItemsQuery query)
            where T : BaseItem
        {
            return GetResult(items, null, query);
        }

        private QueryResult<BaseItem> GetResult<T>(IEnumerable<T> items,
            int? totalRecordLimit,
            UserItemsQuery query)
            where T : BaseItem
        {
            return SortAndFilter(items, totalRecordLimit, query, _libraryManager, _userDataManager);
        }

        public static QueryResult<BaseItem> SortAndFilter(IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            UserItemsQuery query,
            ILibraryManager libraryManager,
            IUserDataManager userDataManager)
        {
            var user = query.User;

            items = items.Where(i => Filter(i, user, query, userDataManager));

            return Sort(items, totalRecordLimit, query, libraryManager);
        }

        public static QueryResult<BaseItem> Sort(IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            UserItemsQuery query,
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

        private static bool Filter(BaseItem item, User user, UserItemsQuery query, IUserDataManager userDataManager)
        {
            if (query.MediaTypes.Length > 0 && !query.MediaTypes.Contains(item.MediaType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
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

        private IEnumerable<Folder> GetMediaFolders(User user, string[] viewTypes)
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
    }
}
