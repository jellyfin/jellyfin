using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;

namespace MediaBrowser.Server.Implementations.TV
{
    public class TVSeriesManager : ITVSeriesManager
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;

        public TVSeriesManager(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, IServerConfigurationManager config)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _config = config;
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
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                    limit = 1;
                }
            }

            if (string.IsNullOrWhiteSpace(presentationUniqueKey) && limit.HasValue)
            {
                limit = limit.Value + 10;
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
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                    limit = 1;
                }
            }

            if (string.IsNullOrWhiteSpace(presentationUniqueKey) && limit.HasValue)
            {
                limit = limit.Value + 10;
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

            var allNextUp = series
                .Select(i => GetNextUp(i, currentUser))
                .Where(i => i.Item1 != null)
                // Include if an episode was found, and either the series is not unwatched or the specific series was requested
                .OrderByDescending(i => i.Item2)
                .ThenByDescending(i => i.Item1.PremiereDate ?? DateTime.MinValue)
                .ToList();

            // If viewing all next up for all series, remove first episodes
            if (string.IsNullOrWhiteSpace(request.SeriesId))
            {
                var withoutFirstEpisode = allNextUp
                    .Where(i => !i.Item3)
                    .ToList();

                // But if that returns empty, keep those first episodes (avoid completely empty view)
                if (withoutFirstEpisode.Count > 0)
                {
                    allNextUp = withoutFirstEpisode;
                }
            }

            return allNextUp
                .Select(i => i.Item1)
                .Take(request.Limit ?? int.MaxValue);
        }

        private string GetUniqueSeriesKey(BaseItem series)
        {
            if (_config.Configuration.SchemaVersion < 97)
            {
                return series.Id.ToString("N");
            }
            return series.PresentationUniqueKey;
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
                AncestorWithPresentationUniqueKey = GetUniqueSeriesKey(series),
                IncludeItemTypes = new[] { typeof(Episode).Name },
                SortBy = new[] { ItemSortBy.SortName },
                SortOrder = SortOrder.Descending,
                IsPlayed = true,
                Limit = 1,
                ParentIndexNumberNotEquals = 0

            }).FirstOrDefault();

            var firstUnwatchedEpisode = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = GetUniqueSeriesKey(series),
                IncludeItemTypes = new[] { typeof(Episode).Name },
                SortBy = new[] { ItemSortBy.SortName },
                SortOrder = SortOrder.Ascending,
                Limit = 1,
                IsPlayed = false,
                IsVirtualItem = false,
                ParentIndexNumberNotEquals = 0,
                MinSortName = lastWatchedEpisode == null ? null : lastWatchedEpisode.SortName

            }).Cast<Episode>().FirstOrDefault();

            if (lastWatchedEpisode != null && firstUnwatchedEpisode != null)
            {
                var userData = _userDataManager.GetUserData(user, lastWatchedEpisode);

                var lastWatchedDate = userData.LastPlayedDate ?? DateTime.MinValue.AddDays(1);

                return new Tuple<Episode, DateTime, bool>(firstUnwatchedEpisode, lastWatchedDate, false);
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
