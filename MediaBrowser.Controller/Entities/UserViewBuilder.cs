using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    public class UserViewBuilder
    {
        private readonly IUserViewManager _userViewManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly ITVSeriesManager _tvSeriesManager;
        private readonly IServerConfigurationManager _config;

        public UserViewBuilder(
            IUserViewManager userViewManager,
            ILibraryManager libraryManager,
            ILogger logger,
            IUserDataManager userDataManager,
            ITVSeriesManager tvSeriesManager,
            IServerConfigurationManager config)
        {
            _userViewManager = userViewManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _tvSeriesManager = tvSeriesManager;
            _config = config;
        }

        public QueryResult<BaseItem> GetUserItems(Folder queryParent, Folder displayParent, string viewType, InternalItemsQuery query)
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
                case CollectionType.Folders:
                    return GetResult(_libraryManager.GetUserRootFolder().GetChildren(user, true), queryParent, query);

                case CollectionType.TvShows:
                    return GetTvView(queryParent, user, query);

                case CollectionType.Movies:
                    return GetMovieFolders(queryParent, user, query);

                case SpecialFolder.TvShowSeries:
                    return GetTvSeries(queryParent, user, query);

                case SpecialFolder.TvGenres:
                    return GetTvGenres(queryParent, user, query);

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
                    return GetMovieGenres(queryParent, user, query);

                case SpecialFolder.MovieGenre:
                    return GetMovieGenreItems(queryParent, displayParent, user, query);

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

                default:
                    {
                        if (queryParent is UserView)
                        {
                            return GetResult(GetMediaFolders(user).OfType<Folder>().SelectMany(i => i.GetChildren(user, true)), queryParent, query);
                        }

                        return queryParent.GetItems(query);
                    }
            }
        }

        private int GetSpecialItemsLimit()
        {
            return 50;
        }

        private QueryResult<BaseItem> GetMovieFolders(Folder parent, User user, InternalItemsQuery query)
        {
            if (query.Recursive)
            {
                query.Recursive = true;
                query.SetUser(user);

                if (query.IncludeItemTypes.Length == 0)
                {
                    query.IncludeItemTypes = new[] { typeof(Movie).Name };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>();

            list.Add(GetUserView(SpecialFolder.MovieResume, "HeaderContinueWatching", "0", parent));
            list.Add(GetUserView(SpecialFolder.MovieLatest, "Latest", "1", parent));
            list.Add(GetUserView(SpecialFolder.MovieMovies, "Movies", "2", parent));
            list.Add(GetUserView(SpecialFolder.MovieCollections, "Collections", "3", parent));
            list.Add(GetUserView(SpecialFolder.MovieFavorites, "Favorites", "4", parent));
            list.Add(GetUserView(SpecialFolder.MovieGenres, "Genres", "5", parent));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetFavoriteMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteEpisodes(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { typeof(Episode).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieCollections(Folder parent, User user, InternalItemsQuery query)
        {
            query.Parent = null;
            query.IncludeItemTypes = new[] { typeof(BoxSet).Name };
            query.SetUser(user);
            query.Recursive = true;

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray();

            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetMovieResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray();
            query.IsResumable = true;
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> ConvertToResult(List<BaseItem> items)
        {
            var arr = items.ToArray();
            return new QueryResult<BaseItem>
            {
                Items = arr,
                TotalRecordCount = arr.Length
            };
        }

        private QueryResult<BaseItem> GetMovieGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = parent.QueryRecursive(new InternalItemsQuery(user)
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting genre");
                        return null;
                    }

                })
                .Where(i => i != null)
                .Select(i => GetUserViewWithName(i.Name, SpecialFolder.MovieGenre, i.SortName, parent));

            return GetResult(genres, parent, query);
        }

        private QueryResult<BaseItem> GetMovieGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = queryParent;
            query.GenreIds = new[] { displayParent.Id };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Movie).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetTvView(Folder parent, User user, InternalItemsQuery query)
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

            list.Add(GetUserView(SpecialFolder.TvResume, "HeaderContinueWatching", "0", parent));
            list.Add(GetUserView(SpecialFolder.TvNextUp, "HeaderNextUp", "1", parent));
            list.Add(GetUserView(SpecialFolder.TvLatest, "Latest", "2", parent));
            list.Add(GetUserView(SpecialFolder.TvShowSeries, "Shows", "3", parent));
            list.Add(GetUserView(SpecialFolder.TvFavoriteSeries, "HeaderFavoriteShows", "4", parent));
            list.Add(GetUserView(SpecialFolder.TvFavoriteEpisodes, "HeaderFavoriteEpisodes", "5", parent));
            list.Add(GetUserView(SpecialFolder.TvGenres, "Genres", "6", parent));

            return GetResult(list, parent, query);
        }

        private QueryResult<BaseItem> GetTvLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { ItemSortBy.DateCreated, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray();

            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Episode).Name };
            query.IsVirtualItem = false;

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetTvNextUp(Folder parent, InternalItemsQuery query)
        {
            var parentFolders = GetMediaFolders(parent, query.User, new[] { CollectionType.TvShows, string.Empty });

            var result = _tvSeriesManager.GetNextUp(new NextUpQuery
            {
                Limit = query.Limit,
                StartIndex = query.StartIndex,
                UserId = query.User.Id

            }, parentFolders, query.DtoOptions);

            return result;
        }

        private QueryResult<BaseItem> GetTvResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { ItemSortBy.DatePlayed, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Descending)).ToArray();
            query.IsResumable = true;
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { typeof(Episode).Name };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetTvSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetTvGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = parent.QueryRecursive(new InternalItemsQuery(user)
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting genre");
                        return null;
                    }

                })
                .Where(i => i != null)
                .Select(i => GetUserViewWithName(i.Name, SpecialFolder.TvGenre, i.SortName, parent));

            return GetResult(genres, parent, query);
        }

        private QueryResult<BaseItem> GetTvGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = queryParent;
            query.GenreIds = new[] { displayParent.Id };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { typeof(Series).Name };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetResult<T>(QueryResult<T> result)
            where T : BaseItem
        {
            return new QueryResult<BaseItem>
            {
                Items = result.Items, //TODO Fix The co-variant conversion between T[] and BaseItem[], this can generate runtime issues if T is not BaseItem.
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

        public static QueryResult<BaseItem> PostFilterAndSort(IEnumerable<BaseItem> items,
            BaseItem queryParent,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager,
            IServerConfigurationManager configurationManager)
        {
            var user = query.User;

            // This must be the last filter
            if (!string.IsNullOrEmpty(query.AdjacentTo))
            {
                items = FilterForAdjacency(items.ToList(), query.AdjacentTo);
            }

            return SortAndPage(items, totalRecordLimit, query, libraryManager, true);
        }

        public static QueryResult<BaseItem> SortAndPage(
            IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager,
            bool enableSorting)
        {
            if (enableSorting)
            {
                if (query.OrderBy.Count > 0)
                {
                    items = libraryManager.Sort(items, query.User, query.OrderBy);
                }
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

            if (query.IsVirtualItem.HasValue && item.IsVirtualItem != query.IsVirtualItem.Value)
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
                userData = userDataManager.GetUserData(user, item);

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

            /*
             * fuck - fix this
            if (query.IsHD.HasValue)
            {
                if (item.IsHD != query.IsHD.Value)
                {
                    return false;
                }
            }
            */

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
                        ? movie.SpecialFeatureIds.Length > 0
                        : movie.SpecialFeatureIds.Length == 0;

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

                var themeCount = item.ThemeSongIds.Length;
                var ok = filterValue ? themeCount > 0 : themeCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            if (query.HasThemeVideo.HasValue)
            {
                var filterValue = query.HasThemeVideo.Value;

                var themeCount = item.ThemeVideoIds.Length;
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
                if (!query.ItemIds.Contains(item.Id))
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

                if (!(item.CriticRating.HasValue && item.CriticRating >= val))
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

            return true;
        }

        private IEnumerable<BaseItem> GetMediaFolders(User user)
        {
            if (user == null)
            {
                return _libraryManager.RootFolder
                    .Children
                    .OfType<Folder>()
                    .Where(UserView.IsEligibleForGrouping);
            }
            return _libraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .OfType<Folder>()
                .Where(i => user.IsFolderGrouped(i.Id) && UserView.IsEligibleForGrouping(i));
        }

        private BaseItem[] GetMediaFolders(User user, IEnumerable<string> viewTypes)
        {
            if (user == null)
            {
                return GetMediaFolders(null)
                    .Where(i =>
                    {
                        var folder = i as ICollectionFolder;

                        return folder != null && viewTypes.Contains(folder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                    }).ToArray();
            }
            return GetMediaFolders(user)
                .Where(i =>
                {
                    var folder = i as ICollectionFolder;

                    return folder != null && viewTypes.Contains(folder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
                }).ToArray();
        }

        private BaseItem[] GetMediaFolders(Folder parent, User user, IEnumerable<string> viewTypes)
        {
            if (parent == null || parent is UserView)
            {
                return GetMediaFolders(user, viewTypes);
            }

            return new BaseItem[] { parent };
        }

        private UserView GetUserViewWithName(string name, string type, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(parent.Id, parent.Id.ToString("N", CultureInfo.InvariantCulture), type, sortName);
        }

        private UserView GetUserView(string type, string localizationKey, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(parent.Id, type, localizationKey, sortName);
        }

        public static IEnumerable<BaseItem> FilterForAdjacency(List<BaseItem> list, string adjacentToId)
        {
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
