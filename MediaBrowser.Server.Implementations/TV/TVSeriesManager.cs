using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.TV
{
    public class TVSeriesManager : ITVSeriesManager
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        public TVSeriesManager(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var parentIdGuid = string.IsNullOrWhiteSpace(request.ParentId) ? (Guid?)null : new Guid(request.ParentId);

            string presentationUniqueKey = null;
            int? limit = null;
            if (!string.IsNullOrWhiteSpace(request.SeriesId))
            {
                var series = _libraryManager.GetItemById(request.SeriesId);

                if (series != null)
                {
                    presentationUniqueKey = series.PresentationUniqueKey;
                    limit = 1;
                }
            }

            var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                SortOrder = SortOrder.Ascending,
                PresentationUniqueKey = presentationUniqueKey,
                Limit = limit,
                ParentId = parentIdGuid,
                Recursive = true

            }).Cast<Series>();

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items);

            return GetResult(episodes, null, request);
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request, IEnumerable<Folder> parentsFolders)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            string presentationUniqueKey = null;
            int? limit = null;
            if (!string.IsNullOrWhiteSpace(request.SeriesId))
            {
                var series = _libraryManager.GetItemById(request.SeriesId);

                if (series != null)
                {
                    presentationUniqueKey = series.PresentationUniqueKey;
                    limit = 1;
                }
            }

            var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                SortOrder = SortOrder.Ascending,
                PresentationUniqueKey = presentationUniqueKey,
                Limit = limit

            }, parentsFolders.Select(i => i.Id.ToString("N"))).Cast<Series>();

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items);

            return GetResult(episodes, null, request);
        }

        public IEnumerable<Episode> GetNextUpEpisodes(NextUpQuery request, User user, IEnumerable<Series> series)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            return series
                .Select(i => GetNextUp(i, currentUser))
                // Include if an episode was found, and either the series is not unwatched or the specific series was requested
                .Where(i => i.Item1 != null && (!i.Item3 || !string.IsNullOrWhiteSpace(request.SeriesId)))
                .OrderByDescending(i => i.Item2)
                .ThenByDescending(i => i.Item1.PremiereDate ?? DateTime.MinValue)
                .Select(i => i.Item1);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{Episode}.</returns>
        private Tuple<Episode, DateTime, bool> GetNextUp(Series series, User user)
        {
            var lastWatchedEpisode = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = series.PresentationUniqueKey,
                IncludeItemTypes = new[] { typeof(Episode).Name },
                SortBy = new[] { ItemSortBy.SortName },
                SortOrder = SortOrder.Descending,
                IsPlayed = true,
                Limit = 1,
                IsVirtualItem = false,
                ParentIndexNumberNotEquals = 0

            }).FirstOrDefault();

            var firstUnwatchedEpisode = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = series.PresentationUniqueKey,
                IncludeItemTypes = new[] { typeof(Episode).Name },
                SortBy = new[] { ItemSortBy.SortName },
                SortOrder = SortOrder.Ascending,
                Limit = 1,
                IsPlayed = false,
                IsVirtualItem = false,
                ParentIndexNumberNotEquals = 0,
                MinSortName = lastWatchedEpisode == null ? null : lastWatchedEpisode.SortName

            }).Cast<Episode>().FirstOrDefault();

            if (lastWatchedEpisode != null)
            {
                var userData = _userDataManager.GetUserData(user, lastWatchedEpisode);

                if (userData.LastPlayedDate.HasValue)
                {
                    return new Tuple<Episode, DateTime, bool>(firstUnwatchedEpisode, userData.LastPlayedDate.Value, false);
                }
            }

            // Return the first episode
            return new Tuple<Episode, DateTime, bool>(firstUnwatchedEpisode, DateTime.MinValue, true);
        }

        private QueryResult<BaseItem> GetResult(IEnumerable<BaseItem> items, int? totalRecordLimit, NextUpQuery query)
        {
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
    }
}
