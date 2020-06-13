#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.TV
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

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request, DtoOptions dtoOptions)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            string presentationUniqueKey = null;
            if (!string.IsNullOrEmpty(request.SeriesId))
            {
                var series = _libraryManager.GetItemById(request.SeriesId) as Series;

                if (series != null)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetResult(GetNextUpEpisodes(request, user, new[] { presentationUniqueKey }, dtoOptions), request);
            }

            var parentIdGuid = string.IsNullOrEmpty(request.ParentId) ? (Guid?)null : new Guid(request.ParentId);

            BaseItem[] parents;

            if (parentIdGuid.HasValue)
            {
                var parent = _libraryManager.GetItemById(parentIdGuid.Value);

                if (parent != null)
                {
                    parents = new[] { parent };
                }
                else
                {
                    parents = Array.Empty<BaseItem>();
                }
            }
            else
            {
                parents = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                   .Where(i => i is Folder)
                   .Where(i => !user.Configuration.LatestItemsExcludes.Contains(i.Id.ToString("N", CultureInfo.InvariantCulture)))
                   .ToArray();
            }

            return GetNextUp(request, parents, dtoOptions);
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request, BaseItem[] parentsFolders, DtoOptions dtoOptions)
        {
            var user = _userManager.GetUserById(request.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            string presentationUniqueKey = null;
            int? limit = null;
            if (!string.IsNullOrEmpty(request.SeriesId))
            {
                var series = _libraryManager.GetItemById(request.SeriesId) as Series;

                if (series != null)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                    limit = 1;
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetResult(GetNextUpEpisodes(request, user, new[] { presentationUniqueKey }, dtoOptions), request);
            }

            if (limit.HasValue)
            {
                limit = limit.Value + 10;
            }

            var items = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Episode).Name },
                OrderBy = new[] { new ValueTuple<string, SortOrder>(ItemSortBy.DatePlayed, SortOrder.Descending) },
                SeriesPresentationUniqueKey = presentationUniqueKey,
                Limit = limit,
                DtoOptions = new DtoOptions
                {
                    Fields = new ItemFields[]
                    {
                        ItemFields.SeriesPresentationUniqueKey
                    },
                    EnableImages = false
                },
                GroupBySeriesPresentationUniqueKey = true

            }, parentsFolders.ToList()).Cast<Episode>().Select(GetUniqueSeriesKey);

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items, dtoOptions);

            return GetResult(episodes, request);
        }

        public IEnumerable<Episode> GetNextUpEpisodes(NextUpQuery request, User user, IEnumerable<string> seriesKeys, DtoOptions dtoOptions)
        {
            // Avoid implicitly captured closure
            var currentUser = user;

            var allNextUp = seriesKeys
                .Select(i => GetNextUp(i, currentUser, dtoOptions));

            //allNextUp = allNextUp.OrderByDescending(i => i.Item1);

            // If viewing all next up for all series, remove first episodes
            // But if that returns empty, keep those first episodes (avoid completely empty view)
            var alwaysEnableFirstEpisode = !string.IsNullOrEmpty(request.SeriesId);
            var anyFound = false;

            return allNextUp
                .Where(i =>
                {
                    if (alwaysEnableFirstEpisode || i.Item1 != DateTime.MinValue)
                    {
                        anyFound = true;
                        return true;
                    }

                    if (!anyFound && i.Item1 == DateTime.MinValue)
                    {
                        return true;
                    }

                    return false;
                })
                .Select(i => i.Item2())
                .Where(i => i != null);
        }

        private static string GetUniqueSeriesKey(Episode episode)
        {
            return episode.SeriesPresentationUniqueKey;
        }

        private static string GetUniqueSeriesKey(Series series)
        {
            return series.GetPresentationUniqueKey();
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <returns>Task{Episode}.</returns>
        private Tuple<DateTime, Func<Episode>> GetNextUp(string seriesKey, User user, DtoOptions dtoOptions)
        {
            var lastWatchedEpisode = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                IncludeItemTypes = new[] { typeof(Episode).Name },
                OrderBy = new[] { new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Descending) },
                IsPlayed = true,
                Limit = 1,
                ParentIndexNumberNotEquals = 0,
                DtoOptions = new DtoOptions
                {
                    Fields = new ItemFields[]
                    {
                        ItemFields.SortName
                    },
                    EnableImages = false
                }

            }).FirstOrDefault();

            Func<Episode> getEpisode = () =>
            {
                return _libraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    AncestorWithPresentationUniqueKey = null,
                    SeriesPresentationUniqueKey = seriesKey,
                    IncludeItemTypes = new[] { typeof(Episode).Name },
                    OrderBy = new[] { new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending) },
                    Limit = 1,
                    IsPlayed = false,
                    IsVirtualItem = false,
                    ParentIndexNumberNotEquals = 0,
                    MinSortName = lastWatchedEpisode == null ? null : lastWatchedEpisode.SortName,
                    DtoOptions = dtoOptions

                }).Cast<Episode>().FirstOrDefault();
            };

            if (lastWatchedEpisode != null)
            {
                var userData = _userDataManager.GetUserData(user, lastWatchedEpisode);

                var lastWatchedDate = userData.LastPlayedDate ?? DateTime.MinValue.AddDays(1);

                return new Tuple<DateTime, Func<Episode>>(lastWatchedDate, getEpisode);
            }

            // Return the first episode
            return new Tuple<DateTime, Func<Episode>>(DateTime.MinValue, getEpisode);
        }

        private static QueryResult<BaseItem> GetResult(IEnumerable<BaseItem> items, NextUpQuery query)
        {
            int totalCount = 0;

            if (query.EnableTotalRecordCount)
            {
                var list = items.ToList();
                totalCount = list.Count;
                items = list;
            }

            if (query.StartIndex.HasValue)
            {
                items = items.Skip(query.StartIndex.Value);
            }
            if (query.Limit.HasValue)
            {
                items = items.Take(query.Limit.Value);
            }

            return new QueryResult<BaseItem>
            {
                TotalRecordCount = totalCount,
                Items = items.ToArray()
            };
        }
    }
}
