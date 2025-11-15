#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace MediaBrowser.Controller.Entities
{
    public class UserViewBuilder
    {
        private readonly IUserViewManager _userViewManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<BaseItem> _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly ITVSeriesManager _tvSeriesManager;

        public UserViewBuilder(
            IUserViewManager userViewManager,
            ILibraryManager libraryManager,
            ILogger<BaseItem> logger,
            IUserDataManager userDataManager,
            ITVSeriesManager tvSeriesManager)
        {
            _userViewManager = userViewManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _userDataManager = userDataManager;
            _tvSeriesManager = tvSeriesManager;
        }

        public QueryResult<BaseItem> GetUserItems(Folder queryParent, Folder displayParent, CollectionType? viewType, InternalItemsQuery query)
        {
            var user = query.User;

            // if (query.IncludeItemTypes is not null &&
            //    query.IncludeItemTypes.Length == 1 &&
            //    string.Equals(query.IncludeItemTypes[0], "Playlist", StringComparison.OrdinalIgnoreCase))
            // {
            //    if (!string.Equals(viewType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase))
            //    {
            //        return await FindPlaylists(queryParent, user, query).ConfigureAwait(false);
            //    }
            // }

            switch (viewType)
            {
                case CollectionType.folders:
                    return GetResult(_libraryManager.GetUserRootFolder().GetChildren(user, true), query);

                case CollectionType.tvshows:
                    return GetTvView(queryParent, user, query);

                case CollectionType.movies:
                    return GetMovieFolders(queryParent, user, query);

                case CollectionType.tvshowseries:
                    return GetTvSeries(queryParent, user, query);

                case CollectionType.tvgenres:
                    return GetTvGenres(queryParent, user, query);

                case CollectionType.tvgenre:
                    return GetTvGenreItems(queryParent, displayParent, user, query);

                case CollectionType.tvresume:
                    return GetTvResume(queryParent, user, query);

                case CollectionType.tvnextup:
                    return GetTvNextUp(queryParent, query);

                case CollectionType.tvlatest:
                    return GetTvLatest(queryParent, user, query);

                case CollectionType.moviefavorites:
                    return GetFavoriteMovies(queryParent, user, query);

                case CollectionType.movielatest:
                    return GetMovieLatest(queryParent, user, query);

                case CollectionType.moviegenres:
                    return GetMovieGenres(queryParent, user, query);

                case CollectionType.moviegenre:
                    return GetMovieGenreItems(queryParent, displayParent, user, query);

                case CollectionType.movieresume:
                    return GetMovieResume(queryParent, user, query);

                case CollectionType.moviemovies:
                    return GetMovieMovies(queryParent, user, query);

                case CollectionType.moviecollection:
                    return GetMovieCollections(user, query);

                case CollectionType.tvfavoriteepisodes:
                    return GetFavoriteEpisodes(queryParent, user, query);

                case CollectionType.tvfavoriteseries:
                    return GetFavoriteSeries(queryParent, user, query);

                default:
                    {
                        if (queryParent is UserView)
                        {
                            return GetResult(GetMediaFolders(user).OfType<Folder>().SelectMany(i => i.GetChildren(user, true)), query);
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
                    query.IncludeItemTypes = new[] { BaseItemKind.Movie };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>
            {
                GetUserView(CollectionType.movieresume, "HeaderContinueWatching", "0", parent),
                GetUserView(CollectionType.movielatest, "Latest", "1", parent),
                GetUserView(CollectionType.moviemovies, "Movies", "2", parent),
                GetUserView(CollectionType.moviecollection, "Collections", "3", parent),
                GetUserView(CollectionType.moviefavorites, "Favorites", "4", parent),
                GetUserView(CollectionType.moviegenres, "Genres", "5", parent)
            };

            return GetResult(list, query);
        }

        private QueryResult<BaseItem> GetFavoriteMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { BaseItemKind.Movie };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { BaseItemKind.Series };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetFavoriteEpisodes(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { BaseItemKind.Episode };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieMovies(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { BaseItemKind.Movie };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieCollections(User user, InternalItemsQuery query)
        {
            query.Parent = null;
            query.IncludeItemTypes = new[] { BaseItemKind.BoxSet };
            query.SetUser(user);
            query.Recursive = true;

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetMovieLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Descending) };
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { BaseItemKind.Movie };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetMovieResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Descending) };
            query.IsResumable = true;
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { BaseItemKind.Movie };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> ConvertToResult(IReadOnlyList<BaseItem> items)
        {
            return new QueryResult<BaseItem>(items);
        }

        private QueryResult<BaseItem> GetMovieGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie },
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
                .Where(i => i is not null)
                .Select(i => GetUserViewWithName(CollectionType.moviegenre, i.SortName, parent));

            return GetResult(genres, query);
        }

        private QueryResult<BaseItem> GetMovieGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = queryParent;
            query.GenreIds = new[] { displayParent.Id };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { BaseItemKind.Movie };

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
                    query.IncludeItemTypes = new[]
                    {
                        BaseItemKind.Series,
                        BaseItemKind.Season,
                        BaseItemKind.Episode
                    };
                }

                return parent.QueryRecursive(query);
            }

            var list = new List<BaseItem>
            {
                GetUserView(CollectionType.tvresume, "HeaderContinueWatching", "0", parent),
                GetUserView(CollectionType.tvnextup, "HeaderNextUp", "1", parent),
                GetUserView(CollectionType.tvlatest, "Latest", "2", parent),
                GetUserView(CollectionType.tvshowseries, "Shows", "3", parent),
                GetUserView(CollectionType.tvfavoriteseries, "HeaderFavoriteShows", "4", parent),
                GetUserView(CollectionType.tvfavoriteepisodes, "HeaderFavoriteEpisodes", "5", parent),
                GetUserView(CollectionType.tvgenres, "Genres", "6", parent)
            };

            return GetResult(list, query);
        }

        private QueryResult<BaseItem> GetTvLatest(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Descending) };
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { BaseItemKind.Episode };
            query.IsVirtualItem = false;

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetTvNextUp(Folder parent, InternalItemsQuery query)
        {
            var parentFolders = GetMediaFolders(parent, query.User, new[] { CollectionType.tvshows });

            var result = _tvSeriesManager.GetNextUp(
                new NextUpQuery
                {
                    Limit = query.Limit,
                    StartIndex = query.StartIndex,
                    User = query.User
                },
                parentFolders,
                query.DtoOptions);

            return result;
        }

        private QueryResult<BaseItem> GetTvResume(Folder parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Descending) };
            query.IsResumable = true;
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.Limit = GetSpecialItemsLimit();
            query.IncludeItemTypes = new[] { BaseItemKind.Episode };

            return ConvertToResult(_libraryManager.GetItemList(query));
        }

        private QueryResult<BaseItem> GetTvSeries(Folder parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { BaseItemKind.Series };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetTvGenres(Folder parent, User user, InternalItemsQuery query)
        {
            var genres = parent.QueryRecursive(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
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
                .Where(i => i is not null)
                .Select(i => GetUserViewWithName(CollectionType.tvgenre, i.SortName, parent));

            return GetResult(genres, query);
        }

        private QueryResult<BaseItem> GetTvGenreItems(Folder queryParent, Folder displayParent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = queryParent;
            query.GenreIds = new[] { displayParent.Id };
            query.SetUser(user);

            query.IncludeItemTypes = new[] { BaseItemKind.Series };

            return _libraryManager.GetItemsResult(query);
        }

        private QueryResult<BaseItem> GetResult<T>(
            IEnumerable<T> items,
            InternalItemsQuery query)
            where T : BaseItem
        {
            items = items.Where(i => Filter(i, query.User, query, _userDataManager, _libraryManager));

            return PostFilterAndSort(items, null, query, _libraryManager);
        }

        public static bool FilterItem(BaseItem item, InternalItemsQuery query)
        {
            return Filter(item, query.User, query, BaseItem.UserDataManager, BaseItem.LibraryManager);
        }

        public static QueryResult<BaseItem> PostFilterAndSort(
            IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager)
        {
            // This must be the last filter
            if (!query.AdjacentTo.IsNullOrEmpty())
            {
                items = FilterForAdjacency(items.ToList(), query.AdjacentTo.Value);
            }

            return SortAndPage(items, totalRecordLimit, query, libraryManager);
        }

        public static QueryResult<BaseItem> SortAndPage(
            IEnumerable<BaseItem> items,
            int? totalRecordLimit,
            InternalItemsQuery query,
            ILibraryManager libraryManager)
        {
            if (query.OrderBy.Count > 0)
            {
                items = libraryManager.Sort(items, query.User, query.OrderBy);
            }

            var itemsArray = totalRecordLimit.HasValue ? items.Take(totalRecordLimit.Value).ToArray() : items.ToArray();
            var totalCount = itemsArray.Length;

            if (query.Limit.HasValue && query.Limit.Value > 0)
            {
                itemsArray = itemsArray.Skip(query.StartIndex ?? 0).Take(query.Limit.Value).ToArray();
            }
            else if (query.StartIndex.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex.Value).ToArray();
            }

            return new QueryResult<BaseItem>(
                query.StartIndex,
                totalCount,
                itemsArray);
        }

        public static bool Filter(BaseItem item, User user, InternalItemsQuery query, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (!string.IsNullOrEmpty(query.NameStartsWith) && !item.SortName.StartsWith(query.NameStartsWith, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

#pragma warning disable CA1309 // Use ordinal string comparison
            if (!string.IsNullOrEmpty(query.NameStartsWithOrGreater) && string.Compare(query.NameStartsWithOrGreater, item.SortName, StringComparison.InvariantCultureIgnoreCase) == 1)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(query.NameLessThan) && string.Compare(query.NameLessThan, item.SortName, StringComparison.InvariantCultureIgnoreCase) != 1)
#pragma warning restore CA1309 // Use ordinal string comparison
            {
                return false;
            }

            if (query.MediaTypes.Length > 0 && !query.MediaTypes.Contains(item.MediaType))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length > 0 && !query.IncludeItemTypes.Contains(item.GetBaseItemKind()))
            {
                return false;
            }

            if (query.ExcludeItemTypes.Length > 0 && query.ExcludeItemTypes.Contains(item.GetBaseItemKind()))
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
                userData ??= userDataManager.GetUserData(user, item);
                var isFavoriteOrLiked = userData.IsFavorite || (userData.Likes ?? false);

                if (isFavoriteOrLiked != query.IsFavoriteOrLiked.Value)
                {
                    return false;
                }
            }

            if (query.IsFavorite.HasValue)
            {
                userData ??= userDataManager.GetUserData(user, item);
                if (userData.IsFavorite != query.IsFavorite.Value)
                {
                    return false;
                }
            }

            if (query.IsResumable.HasValue)
            {
                userData ??= userDataManager.GetUserData(user, item);
                var isResumable = userData.PlaybackPositionTicks > 0;

                if (isResumable != query.IsResumable.Value)
                {
                    return false;
                }
            }

            if (query.IsPlayed.HasValue)
            {
                userData ??= userDataManager.GetUserData(user, item);
                if (item.IsPlayed(user, userData) != query.IsPlayed.Value)
                {
                    return false;
                }
            }

            // Filter by Video3DFormat
            if (query.Is3D.HasValue)
            {
                var val = query.Is3D.Value;
                var video = item as Video;

                if (video is null || val != video.Video3DFormat.HasValue)
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

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProvider.Imdb));

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.HasTmdbId.HasValue)
            {
                var filterValue = query.HasTmdbId.Value;

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProvider.Tmdb));

                if (hasValue != filterValue)
                {
                    return false;
                }
            }

            if (query.HasTvdbId.HasValue)
            {
                var filterValue = query.HasTvdbId.Value;

                var hasValue = !string.IsNullOrEmpty(item.GetProviderId(MetadataProvider.Tvdb));

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

                if (item is ISupportsPlaceHolders hasPlaceHolder)
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

                if (item is IHasSpecialFeatures movie)
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

                if (video is null || val != video.HasSubtitles)
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

                if (item is IHasTrailers hasTrailers)
                {
                    trailerCount = hasTrailers.GetTrailerCount();
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

                var themeCount = item.GetThemeSongs(user).Count;
                var ok = filterValue ? themeCount > 0 : themeCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            if (query.HasThemeVideo.HasValue)
            {
                var filterValue = query.HasThemeVideo.Value;

                var themeCount = item.GetThemeVideos(user).Count;
                var ok = filterValue ? themeCount > 0 : themeCount == 0;

                if (!ok)
                {
                    return false;
                }
            }

            // Apply genre filter
            if (query.Genres.Count > 0 && !query.Genres.Any(v => item.Genres.Contains(v, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Filter by VideoType
            if (query.VideoTypes.Length > 0)
            {
                var video = item as Video;
                if (video is null || !query.VideoTypes.Contains(video.VideoType))
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
                return studioItem is not null && item.Studios.Contains(studioItem.Name, StringComparison.OrdinalIgnoreCase);
            }))
            {
                return false;
            }

            // Apply genre filter
            if (query.GenreIds.Count > 0 && !query.GenreIds.Any(id =>
            {
                var genreItem = libraryManager.GetItemById(id);
                return genreItem is not null && item.Genres.Contains(genreItem.Name, StringComparison.OrdinalIgnoreCase);
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
                if (!tags.Any(v => item.Tags.Contains(v, StringComparison.OrdinalIgnoreCase)))
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

                if (episode is null)
                {
                    return false;
                }

                if (!Series.FilterEpisodesBySeason(new[] { episode }, query.AiredDuringSeason.Value, true).Any())
                {
                    return false;
                }
            }

            if (query.ExcludeItemIds.Contains(item.Id))
            {
                return false;
            }

            return true;
        }

        private IEnumerable<BaseItem> GetMediaFolders(User user)
        {
            if (user is null)
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

        private BaseItem[] GetMediaFolders(User user, IEnumerable<CollectionType> viewTypes)
        {
            if (user is null)
            {
                return GetMediaFolders(null)
                    .Where(i =>
                    {
                        var folder = i as ICollectionFolder;

                        return folder?.CollectionType is not null && viewTypes.Contains(folder.CollectionType.Value);
                    }).ToArray();
            }

            return GetMediaFolders(user)
                .Where(i =>
                {
                    var folder = i as ICollectionFolder;

                    return folder?.CollectionType is not null && viewTypes.Contains(folder.CollectionType.Value);
                }).ToArray();
        }

        private BaseItem[] GetMediaFolders(Folder parent, User user, IEnumerable<CollectionType> viewTypes)
        {
            if (parent is null || parent is UserView)
            {
                return GetMediaFolders(user, viewTypes);
            }

            return new BaseItem[] { parent };
        }

        private UserView GetUserViewWithName(CollectionType? type, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(parent.Id, type, parent.Id.ToString("N", CultureInfo.InvariantCulture), sortName);
        }

        private UserView GetUserView(CollectionType? type, string localizationKey, string sortName, BaseItem parent)
        {
            return _userViewManager.GetUserSubView(parent.Id, type, localizationKey, sortName);
        }

        public static IEnumerable<BaseItem> FilterForAdjacency(List<BaseItem> list, Guid adjacentTo)
        {
            var adjacentToItem = list.FirstOrDefault(i => i.Id.Equals(adjacentTo));

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

            return list.Where(i => i.Id.Equals(previousId) || i.Id.Equals(nextId) || i.Id.Equals(adjacentTo));
        }
    }
}
