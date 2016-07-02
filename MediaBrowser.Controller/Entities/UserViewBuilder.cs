using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MoreLinq;

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
        private readonly IServerConfigurationManager _config;
        private readonly IPlaylistManager _playlistManager;

        public UserViewBuilder(IUserViewManager userViewManager, ILiveTvManager liveTvManager, IChannelManager channelManager, ILibraryManager libraryManager, ILogger logger, IUserDataManager userDataManager, ITVSeriesManager tvSeriesManager, IServerConfigurationManager config, IPlaylistManager playlistManager)
        {
            _userViewManager = userViewManager;
            _liveTvManager = liveTvManager;
            _channelManager = channelManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _tvSeriesManager = tvSeriesManager;
            _config = config;
            _playlistManager = playlistManager;
        }

        public async Task<QueryResult<BaseItem>> GetUserItems(Folder queryParent, Folder displayParent, string viewType, InternalItemsQuery query)
        {
            var user = query.User;

            //if (query.IncludeItemTypes != null &&
            //    query.IncludeItemTypes.Length == 1 &&
            //    string.Equals(query.IncludeItemTypes[0], "Playlist", StringComparison.OrdinalIgnoreCase))
            //{
            //    if (!string.Equals(viewType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase))
            //    {
            //        return await FindPlaylists(queryParent, user, query).ConfigureAwait(false);
            //    }
            //}

            switch (viewType)
            {
                case CollectionType.Channels:
                    {
                        var result = await _channelManager.GetChannelsInternal(new ChannelQuery
                        {
                            UserId = user == null ? null : user.Id.ToString("N"),
                            Limit = query.Limit,
                            StartIndex = query.StartIndex

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case SpecialFolder.LiveTvChannels:
                    {
                        var result = await _liveTvManager.GetInternalChannels(new LiveTvChannelQuery
                        {
                            UserId = query.User.Id.ToString("N"),
                            Limit = query.Limit,
                            StartIndex = query.StartIndex

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case SpecialFolder.LiveTvNowPlaying:
                    {
                        var result = await _liveTvManager.GetRecommendedProgramsInternal(new RecommendedProgramQuery
                        {
                            UserId = query.User.Id.ToString("N"),
                            Limit = query.Limit,
                            IsAiring = true

                        }, CancellationToken.None).ConfigureAwait(false);

                        return GetResult(result);
                    }

                case SpecialFolder.LiveTvRecordingGroups:
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
                        return await GetLiveTvView(queryParent, user, query).ConfigureAwait(false);
                    }

                case CollectionType.Photos:
                case CollectionType.Books:
                case CollectionType.HomeVideos:
                case CollectionType.Games:
                case CollectionType.MusicVideos:
                    {
                        if (query.Recursive)
                        {
                            query.Recursive = true;
                            query.ParentId = queryParent.Id;
                            query.SetUser(user);

                            return _libraryManager.GetItemsResult(query);
                        }
                        return GetResult(queryParent.GetChildren(user, true), queryParent, query);
                    }

                case CollectionType.Folders:
                    return GetResult(user.RootFolder.GetChildren(user, true), queryParent, query);

                case CollectionType.Playlists:
                    return await GetPlaylistsView(queryParent, user, query).ConfigureAwait(false);

                case CollectionType.BoxSets:
                    return GetBoxsetView(queryParent, user, query);

                case CollectionType.TvShows:
                    return await GetTvView(queryParent, user, query).ConfigureAwait(false);

                case CollectionType.Movies:
                    return await GetMovieFolders(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.TvShowSeries:
                    return GetTvSeries(queryParent, user, query);

                case SpecialFolder.TvGenres:
                    return await GetTvGenres(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.TvGenre:
                    return GetTvGenreItems(queryParent, displayParent, user, query);

                case SpecialFolder.TvResume:
                    return GetTvResume(queryParent, user, query);

                case SpecialFolder.TvNextUp:
                    return GetTvNextUp(queryParent, query);

                case SpecialFolder.TvLatest:
                    return GetTvLatest(queryParent, user, query);

                case SpecialFolder.MovieFavorites:
                    return GetFavoriteMovies(queryParent, user, query);

                case SpecialFolder.MovieLatest:
                    return GetMovieLatest(queryParent, user, query);

                case SpecialFolder.MovieGenres:
                    return await GetMovieGenres(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MovieGenre:
                    return await GetMovieGenreItems(queryParent, displayParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MovieResume:
                    return GetMovieResume(queryParent, user, query);

                case SpecialFolder.MovieMovies:
                    return GetMovieMovies(queryParent, user, query);

                case SpecialFolder.MovieCollections:
                    return GetMovieCollections(queryParent, user, query);

                case SpecialFolder.TvFavoriteEpisodes:
                    return GetFavoriteEpisodes(queryParent, user, query);

                case SpecialFolder.TvFavoriteSeries:
                    return GetFavoriteSeries(queryParent, user, query);

                case CollectionType.Music:
                    return await GetMusicFolders(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MusicGenres:
                    return await GetMusicGenres(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MusicGenre:
                    return await GetMusicGenreItems(queryParent, displayParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MusicLatest:
                    return GetMusicLatest(queryParent, user, query);

                case SpecialFolder.MusicPlaylists:
                    return GetMusicPlaylists(queryParent, user, query);

                case SpecialFolder.MusicAlbums:
                    return GetMusicAlbums(queryParent, user, query);

                case SpecialFolder.MusicAlbumArtists:
                    return GetMusicAlbumArtists(queryParent, user, query);

                case SpecialFolder.MusicArtists:
                    return GetMusicArtists(queryParent, user, query);

                case SpecialFolder.MusicSongs:
                    return GetMusicSongs(queryParent, user, query);

                case SpecialFolder.MusicFavorites:
                    return await GetMusicFavorites(queryParent, user, query).ConfigureAwait(false);

                case SpecialFolder.MusicFavoriteAlbums:
                    return GetFavoriteAlbums(queryParent, user, query);

                case SpecialFolder.MusicFavoriteArtists:
                    return GetFavoriteArtists(queryParent, user, query);

                case SpecialFolder.MusicFavoriteSongs:
                    return GetFavoriteSongs(queryParent, user, query);

                default:
                    {
                        if (queryParent is UserView)
                        {
                            return GetResult(GetMediaFolders(user).SelectMany(i => i.GetChildren(user, true)), queryParent, query);
                        }
                        else
                        {
                            return GetResult(queryParent.GetChildren(user, true), queryParent, query);
                        }
                    }
            }
        }

        private async Task<QueryResult<BaseItem>> GetMusicFolders(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                query.Recursive = true;
                query.SetUser(user);

                if (query.IncludeItemTypes.Length == 0)
                {
                    query.IncludeItemTypes = new[] { typeof(MusicArtist).Name, typeof(MusicAlbum).Name, typeof(Audio.Audio).Name, typeof(MusicVideo).Name };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>();

            list.Add(await GetUserView(SpecialFolder.MusicLatest, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicPlaylists, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicAlbums, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicAlbumArtists, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicArtists, "4", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicSongs, "5", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicGenres, "6", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicFavorites, "7", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetMusicFavorites(Folder parent, User user, InternalItemsQuery query)
        {
            var list = new List<BaseItem>();

            list.Add(await GetUserView(SpecialFolder.MusicFavoriteAlbums, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicFavoriteArtists, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MusicFavoriteSongs, "2", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetMusicGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var tasks = GetRecursiveChildren(parent, user, new[] { CollectionType.Music, CollectionType.MusicVideos })
                .Where(i => !i.IsFolder)
                .SelectMany(i => i.Genres)
                .DistinctNames()
                .Select(i =>
                {
                    try
                    {
                        return _libraryManager.GetMusicGenre(i);
                    }
                    catch
                    {
                        // Full exception logged at lower levels
                        _logger.Error("Error getting genre");
                        return null;
                    }

                })
                .Where(i => i != null)
                .Select(i => GetUserView(i.Name, SpecialFolder.MusicGenre, i.SortName, parent));

            var genres = await Task.WhenAll(tasks).ConfigureAwait(false);

            return GetResult(genres, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetMusicGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = queryParent.Id;
            query.Genres = new[] { displayParent.Name };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(MusicAlbum).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMusicAlbumArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var items = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parent.Id,
                IncludeItemTypes = new[] { typeof(Audio.Audio).Name },
                EnableTotalRecordCount = false

            }).Items.Cast<IHasAlbumArtist>();

            var artists = _libraryManager.GetAlbumArtists(items);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetMusicArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var items = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parent.Id,
                IncludeItemTypes = new[] { typeof(Audio.Audio).Name, typeof(MusicVideo).Name },
                EnableTotalRecordCount = false

            }).Items.Cast<IHasArtist>();

            var artists = _libraryManager.GetArtists(items);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteArtists(Folder parent, User user, InternalItemsQuery query)
        {
            var items = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parent.Id,
                IncludeItemTypes = new[] { typeof(Audio.Audio).Name },
                EnableTotalRecordCount = false

            }).Items.Cast<IHasAlbumArtist>();

            var artists = _libraryManager.GetAlbumArtists(items).Where(i => _userDataManager.GetUserData(user, i).IsFavorite);

            return GetResult(artists, parent, query);
        }

        private QueryResult<BaseItem> GetMusicPlaylists(Folder parent, User user, InternalItemsQuery query)
        {
            query.ParentId = null;
            query.IncludeItemTypes = new[] { typeof(Playlist).Name };
            query.SetUser(user);
            query.Recursive = true;

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMusicAlbums(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(MusicAlbum).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMusicSongs(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Audio.Audio).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMusicLatest(Folder parent, User user, InternalItemsQuery query)
        {
            var items = _userViewManager.GetLatestItems(new LatestItemsQuery
            {
                UserId = user.Id.ToString("N"),
                Limit = GetSpecialItemsLimit(),
                IncludeItemTypes = new[] { typeof(Audio.Audio).Name },
                ParentId = parent == null ? null : parent.Id.ToString("N"),
                GroupItems = true

            }).Select(i => i.Item1 ?? i.Item2.FirstOrDefault()).Where(i => i != null);

            query.SortBy = new string[] { };

            return PostFilterAndSort(items, parent, null, query);
        }

        private QueryResult<BaseItem> GetFavoriteSongs(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Audio.Audio).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteAlbums(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(MusicAlbum).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private async Task<QueryResult<BaseItem>> FindPlaylists(Folder parent, User user, InternalItemsQuery query)
        {
            var list = _playlistManager.GetPlaylists(user.Id.ToString("N"));

            return GetResult(list, parent, query);
        }

        private int GetSpecialItemsLimit()
        {
            return 50;
        }

        private async Task<QueryResult<BaseItem>> GetMovieFolders(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                query.Recursive = true;
                query.SetUser(user);

                if (query.IncludeItemTypes.Length == 0)
                {
                    query.IncludeItemTypes = new[] { typeof(Movie).Name, typeof(BoxSet).Name };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>();

            list.Add(await GetUserView(SpecialFolder.MovieResume, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MovieLatest, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MovieMovies, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MovieCollections, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MovieFavorites, "4", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.MovieGenres, "5", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteEpisodes(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Episode).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieCollections(Folder parent, User user, InternalItemsQuery query)
        {
            return GetBoxsetView(parent, user, query);
        }

        private QueryResult<BaseItem> GetMovieLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetMovieResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;
            query.IsResumable = true;
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> ConvertToResult(IEnumerable<BaseItem> items)
        {
            var arr = items.ToArray();
            return new QueryResult<BaseItem>
            {
                Items = arr,
                TotalRecordCount = arr.Length
            };
        }

        private async Task<QueryResult<BaseItem>> GetMovieGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var tasks = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Movie).Name },
                Recursive = true,
                EnableTotalRecordCount = false

            }).Items
                .SelectMany(i => i.Genres)
                .DistinctNames()
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
                .Where(i => i != null)
                .Select(i => GetUserView(i.Name, SpecialFolder.MovieGenre, i.SortName, parent));

            var genres = await Task.WhenAll(tasks).ConfigureAwait(false);

            return GetResult(genres, parent, query);
        }

        private async Task<QueryResult<BaseItem>> GetMovieGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = queryParent.Id;
            query.Genres = new[] { displayParent.Name };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private async Task<QueryResult<BaseItem>> GetPlaylistsView(Folder parent, User user, InternalItemsQuery query)
        {
            return GetResult(_playlistManager.GetPlaylists(user.Id.ToString("N")), parent, query);
        }

        private QueryResult<BaseItem> GetBoxsetView(Folder parent, User user, InternalItemsQuery query)
        {
            query.ParentId = null;
            query.IncludeItemTypes = new[] { typeof(BoxSet).Name };
            query.SetUser(user);
            query.Recursive = true;

            return _libraryManager.GetItemsResult(query);
        }

        private async Task<QueryResult<BaseItem>> GetTvView(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                query.Recursive = true;
                query.SetUser(user);

                if (query.IncludeItemTypes.Length == 0)
                {
                    query.IncludeItemTypes = new[] { typeof(Series).Name, typeof(Season).Name, typeof(Episode).Name };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>();

            list.Add(await GetUserView(SpecialFolder.TvResume, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvNextUp, "1", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvLatest, "2", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvShowSeries, "3", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvFavoriteSeries, "4", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvFavoriteEpisodes, "5", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.TvGenres, "6", parent).ConfigureAwait(false));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetTvLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.SortBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName };
            query.SortOrder = SortOrder.Descending;

            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Episode).Name };
            query.ExcludeLocationTypes = new[] { LocationType.Virtual };

            return ConvertToResult(_libraryManager.GetItemList(query));
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
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Episode).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetTvSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = parent.Id;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private async Task<QueryResult<BaseItem>> GetTvGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var tasks = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                Recursive = true,
                EnableTotalRecordCount = false

            }).Items
                .SelectMany(i => i.Genres)
                .DistinctNames()
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
                .Where(i => i != null)
                .Select(i => GetUserView(i.Name, SpecialFolder.TvGenre, i.SortName, parent));

            var genres = await Task.WhenAll(tasks).ConfigureAwait(false);

            return GetResult(genres, parent, query);
        }

        private QueryResult<BaseItem> GetTvGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.ParentId = queryParent.Id;
            query.Genres = new[] { displayParent.Name };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
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
            BaseItem queryParent,
            InternalItemsQuery query)
            where T : BaseItem
        {
            items = items.Where(i => Filter(i, query.User, query, _userDataManager, _libraryManager));

            return PostFilterAndSort(items, queryParent, null, query, _libraryManager, _config);
        }

        public static bool FilterItem(BaseItem item, InternalItemsQuery query)
        {
            return Filter(item, query.User, query, BaseItem.UserDataManager, BaseItem.LibraryManager);
        }

        private QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items,
            BaseItem queryParent,
            int? totalRecordLimit,
            InternalItemsQuery query)
        {
            return PostFilterAndSort(items, queryParent, totalRecordLimit, query, _libraryManager, _config);
        }

        public static QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items,
            BaseItem queryParent,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager,
            IServerConfigurationManager configurationManager)
        {
            var user = query.User;

            items = FilterVirtualEpisodes(items,
                query.IsMissing,
                query.IsVirtualUnaired,
                query.IsUnaired);

            items = CollapseBoxSetItemsIfNeeded(items, query, queryParent, user, configurationManager);

            // This must be the last filter
            if (!string.IsNullOrEmpty(query.AdjacentTo))
            {
                items = FilterForAdjacency(items, query.AdjacentTo);
            }

            return Sort(items, totalRecordLimit, query, libraryManager);
        }

        public static IEnumerable<BaseItem> CollapseBoxSetItemsIfNeeded(IEnumerable<BaseItem> items,
            InternalItemsQuery query,
            BaseItem queryParent,
            User user,
            IServerConfigurationManager configurationManager)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (CollapseBoxSetItems(query, queryParent, user, configurationManager))
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

        public static bool CollapseBoxSetItems(InternalItemsQuery query,
            BaseItem queryParent,
            User user,
            IServerConfigurationManager configurationManager)
        {
            // Could end up stuck in a loop like this
            if (queryParent is BoxSet)
            {
                return false;
            }

            var param = query.CollapseBoxSetItems;

            if (!param.HasValue)
            {
                if (user != null && !configurationManager.Configuration.EnableGroupingIntoCollections)
                {
                    return false;
                }

                if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains("Movie", StringComparer.OrdinalIgnoreCase))
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

            if (!string.IsNullOrWhiteSpace(request.Person))
            {
                return false;
            }

            if (request.PersonIds.Length > 0)
            {
                return false;
            }

            if (request.ItemIds.Length > 0)
            {
                return false;
            }

            if (request.Studios.Length > 0)
            {
                return false;
            }

            if (request.StudioIds.Length > 0)
            {
                return false;
            }

            if (request.GenreIds.Length > 0)
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

            if (request.Tags.Length > 0)
            {
                return false;
            }

            if (request.OfficialRatings.Length > 0)
            {
                return false;
            }

            if (request.MinPlayers.HasValue)
            {
                return false;
            }

            if (request.MaxPlayers.HasValue)
            {
                return false;
            }

            if (request.MinCommunityRating.HasValue)
            {
                return false;
            }

            if (request.MinCriticRating.HasValue)
            {
                return false;
            }

            if (request.MinIndexNumber.HasValue)
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<BaseItem> FilterVirtualEpisodes(
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
            if (isMissing.HasValue)
            {
                var val = isMissing.Value;
                items = items.Where(i =>
                {
                    var e = i as Season;
                    if (e != null)
                    {
                        return (e.IsMissingSeason) == val;
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

            items = items.DistinctBy(i => i.PresentationUniqueKey, StringComparer.OrdinalIgnoreCase);

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

        public static bool Filter(BaseItem item, User user, InternalItemsQuery query, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (query.ItemIdsFromPersonFilters == null)
            {
                if (query.PersonIds.Length > 0)
                {
                    var names = query.PersonIds
                        .Select(libraryManager.GetItemById)
                        .Select(i => i == null ? null : i.Name)
                        .Where(i => !string.IsNullOrWhiteSpace(i))
                        .ToList();

                    var itemIdList = new List<Guid>();
                    foreach (var name in names)
                    {
                        itemIdList.AddRange(libraryManager.GetItemIds(new InternalItemsQuery
                        {
                            Person = name
                        }));
                    }
                    query.ItemIdsFromPersonFilters = itemIdList;
                }

                // Apply person filter
                else if (!string.IsNullOrWhiteSpace(query.Person))
                {
                    var itemIdList = new List<Guid>();

                    itemIdList.AddRange(libraryManager.GetItemIds(new InternalItemsQuery
                    {
                        Person = query.Person,
                        PersonTypes = query.PersonTypes
                    }));
                    query.ItemIdsFromPersonFilters = itemIdList;
                }
            }

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

            if (query.ExcludeLocationTypes.Length > 0 && query.ExcludeLocationTypes.Contains(item.LocationType))
            {
                return false;
            }

            if (query.IsFolder.HasValue && query.IsFolder.Value != item.IsFolder)
            {
                return false;
            }

            UserItemData userData = null;

            if (query.IsLiked.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user, item);

                if (!userData.Likes.HasValue || userData.Likes != query.IsLiked.Value)
                {
                    return false;
                }
            }

            if (query.IsFavoriteOrLiked.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user, item);
                var isFavoriteOrLiked = userData.IsFavorite || (userData.Likes ?? false);

                if (isFavoriteOrLiked != query.IsFavoriteOrLiked.Value)
                {
                    return false;
                }
            }

            if (query.IsFavorite.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user, item);

                if (userData.IsFavorite != query.IsFavorite.Value)
                {
                    return false;
                }
            }

            if (query.IsResumable.HasValue)
            {
                userData = userData ?? userDataManager.GetUserData(user, item);
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
                if (item.GetParents().OfType<BoxSet>().Any() != val)
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

                if (video == null || !video.IsHD.HasValue || val != video.IsHD)
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
                    trailerCount = hasTrailers.GetTrailerIds().Count;
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
            if (query.Genres.Length > 0 && !query.Genres.Any(v => item.Genres.Contains(v, StringComparer.OrdinalIgnoreCase)))
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

            // Apply studio filter
            if (query.StudioIds.Length > 0 && !query.StudioIds.Any(id =>
            {
                var studioItem = libraryManager.GetItemById(id);
                return studioItem != null && item.Studios.Contains(studioItem.Name, StringComparer.OrdinalIgnoreCase);
            }))
            {
                return false;
            }

            // Apply genre filter
            if (query.GenreIds.Length > 0 && !query.GenreIds.Any(id =>
            {
                var genreItem = libraryManager.GetItemById(id);
                return genreItem != null && item.Genres.Contains(genreItem.Name, StringComparer.OrdinalIgnoreCase);
            }))
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

            // Apply official rating filter
            if (query.OfficialRatings.Length > 0 && !query.OfficialRatings.Contains(item.OfficialRating ?? string.Empty))
            {
                return false;
            }

            if (query.ItemIds.Length > 0)
            {
                if (!query.ItemIds.Contains(item.Id.ToString("N"), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Apply person filter
            if (query.ItemIdsFromPersonFilters != null)
            {
                if (!query.ItemIdsFromPersonFilters.Contains(item.Id))
                {
                    return false;
                }
            }

            // Apply tag filter
            var tags = query.Tags;
            if (tags.Length > 0)
            {
                if (!tags.Any(v => item.Tags.Contains(v, StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            if (query.MinPlayers.HasValue)
            {
                var filterValue = query.MinPlayers.Value;

                var game = item as Game;

                if (game != null)
                {
                    var players = game.PlayersSupported ?? 1;

                    var ok = players >= filterValue;

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

            if (query.MaxPlayers.HasValue)
            {
                var filterValue = query.MaxPlayers.Value;

                var game = item as Game;

                if (game != null)
                {
                    var players = game.PlayersSupported ?? 1;

                    var ok = players <= filterValue;

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

            if (query.MinCommunityRating.HasValue)
            {
                var val = query.MinCommunityRating.Value;

                if (!(item.CommunityRating.HasValue && item.CommunityRating >= val))
                {
                    return false;
                }
            }

            if (query.MinCriticRating.HasValue)
            {
                var val = query.MinCriticRating.Value;

                var hasCriticRating = item as IHasCriticRating;

                if (hasCriticRating != null)
                {
                    if (!(hasCriticRating.CriticRating.HasValue && hasCriticRating.CriticRating >= val))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (query.MinIndexNumber.HasValue)
            {
                var val = query.MinIndexNumber.Value;

                if (!(item.IndexNumber.HasValue && item.IndexNumber.Value >= val))
                {
                    return false;
                }
            }

            if (query.MinPremiereDate.HasValue)
            {
                var val = query.MinPremiereDate.Value;

                if (!(item.PremiereDate.HasValue && item.PremiereDate.Value >= val))
                {
                    return false;
                }
            }

            if (query.MaxPremiereDate.HasValue)
            {
                var val = query.MaxPremiereDate.Value;

                if (!(item.PremiereDate.HasValue && item.PremiereDate.Value <= val))
                {
                    return false;
                }
            }

            if (query.ParentIndexNumber.HasValue)
            {
                var filterValue = query.ParentIndexNumber.Value;

                if (item.ParentIndexNumber.HasValue && item.ParentIndexNumber.Value != filterValue)
                {
                    return false;
                }
            }

            if (query.AirDays.Length > 0)
            {
                var ok = new[] { item }.OfType<Series>().Any(p => p.AirDays != null && query.AirDays.Any(d => p.AirDays.Contains(d)));
                if (!ok)
                {
                    return false;
                }
            }

            if (query.SeriesStatuses.Length > 0)
            {
                var ok = new[] { item }.OfType<Series>().Any(p => p.Status.HasValue && query.SeriesStatuses.Contains(p.Status.Value));
                if (!ok)
                {
                    return false;
                }
            }

            if (query.AiredDuringSeason.HasValue)
            {
                var episode = item as Episode;

                if (episode == null)
                {
                    return false;
                }

                if (!Series.FilterEpisodesBySeason(new[] { episode }, query.AiredDuringSeason.Value, true).Any())
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(query.AlbumArtistStartsWithOrGreater))
            {
                var ok = new[] { item }.OfType<IHasAlbumArtist>()
                    .Any(p => string.Compare(query.AlbumArtistStartsWithOrGreater, p.AlbumArtists.FirstOrDefault(), StringComparison.CurrentCultureIgnoreCase) < 1);

                if (!ok)
                {
                    return false;
                }
            }

            // Artists
            if (query.ArtistNames.Length > 0)
            {
                var audio = item as IHasArtist;

                if (!(audio != null && query.ArtistNames.Any(audio.HasAnyArtist)))
                {
                    return false;
                }
            }

            // Albums
            if (query.AlbumNames.Length > 0)
            {
                var audio = item as Audio.Audio;

                if (audio != null)
                {
                    if (!query.AlbumNames.Any(a => string.Equals(a, audio.Album, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                var album = item as MusicAlbum;

                if (album != null)
                {
                    if (!query.AlbumNames.Any(a => string.Equals(a, album.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                var musicVideo = item as MusicVideo;

                if (musicVideo != null)
                {
                    if (!query.AlbumNames.Any(a => string.Equals(a, musicVideo.Album, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }

                return false;
            }

            return true;
        }

        private IEnumerable<Folder> GetMediaFolders(User user)
        {
            if (user == null)
            {
                return _libraryManager.RootFolder
                    .Children
                    .OfType<Folder>()
                    .Where(UserView.IsEligibleForGrouping);
            }
            return user.RootFolder
                .GetChildren(user, true)
                .OfType<Folder>()
                .Where(i => user.IsFolderGrouped(i.Id) && UserView.IsEligibleForGrouping(i));
        }

        private IEnumerable<Folder> GetMediaFolders(User user, IEnumerable<string> viewTypes)
        {
            if (user == null)
            {
                return GetMediaFolders(null)
                    .Where(i =>
                    {
                        var folder = i as ICollectionFolder;

                        return folder != null && viewTypes.Contains(folder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                    });
            }
            return GetMediaFolders(user)
                .Where(i =>
                {
                    var folder = i as ICollectionFolder;

                    return folder != null && viewTypes.Contains(folder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                });
        }

        private IEnumerable<Folder> GetMediaFolders(Folder parent, User user, IEnumerable<string> viewTypes)
        {
            if (parent == null || parent is UserView)
            {
                return GetMediaFolders(user, viewTypes);
            }

            return new[] { parent };
        }

        private IEnumerable<BaseItem> GetRecursiveChildren(Folder parent, User user, IEnumerable<string> viewTypes)
        {
            if (parent == null || parent is UserView)
            {
                if (user == null)
                {
                    return GetMediaFolders(null, viewTypes).SelectMany(i => i.GetRecursiveChildren());
                }

                return GetMediaFolders(user, viewTypes).SelectMany(i => i.GetRecursiveChildren(user));
            }

            if (user == null)
            {
                return parent.GetRecursiveChildren();
            }

            return parent.GetRecursiveChildren(user);
        }

        private async Task<QueryResult<BaseItem>> GetLiveTvView(Folder queryParent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                return await _liveTvManager.GetInternalRecordings(new RecordingQuery
                {
                    IsInProgress = false,
                    Status = RecordingStatus.Completed,
                    UserId = user.Id.ToString("N")

                }, CancellationToken.None).ConfigureAwait(false);
            }

            var list = new List<BaseItem>();

            //list.Add(await GetUserSubView(SpecialFolder.LiveTvNowPlaying, user, "0", parent).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.LiveTvChannels, string.Empty, user.RootFolder).ConfigureAwait(false));
            list.Add(await GetUserView(SpecialFolder.LiveTvRecordingGroups, string.Empty, user.RootFolder).ConfigureAwait(false));

            return GetResult(list, queryParent, query);
        }

        private Task<UserView> GetUserView(string name, string type, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(name, parent.Id.ToString("N"), type, sortName, CancellationToken.None);
        }

        private Task<UserView> GetUserView(string type, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(parent.Id.ToString("N"), type, sortName, CancellationToken.None);
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
