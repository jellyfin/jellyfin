#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Querying;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Emby.Server.Implementations.TV
{
    public class TVSeriesManager : ITVSeriesManager
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _configurationManager;

        public TVSeriesManager(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, IServerConfigurationManager configurationManager)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _configurationManager = configurationManager;
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery query, DtoOptions options)
        {
            var user = _userManager.GetUserById(query.UserId);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            string presentationUniqueKey = null;
            if (!string.IsNullOrEmpty(query.SeriesId))
            {
                if (_libraryManager.GetItemById(query.SeriesId) is Series series)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetResult(GetNextUpEpisodes(query, user, new[] { presentationUniqueKey }, options), query);
            }

            BaseItem[] parents;

            if (query.ParentId.HasValue)
            {
                var parent = _libraryManager.GetItemById(query.ParentId.Value);

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
                   .Where(i => !user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes).Contains(i.Id))
                   .ToArray();
            }

            return GetNextUp(query, parents, options);
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery request, BaseItem[] parentsFolders, DtoOptions options)
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
                if (_libraryManager.GetItemById(request.SeriesId) is Series series)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                    limit = 1;
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetResult(GetNextUpEpisodes(request, user, new[] { presentationUniqueKey }, options), request);
            }

            if (limit.HasValue)
            {
                limit = limit.Value + 10;
            }

            var items = _libraryManager
                .GetItemList(
                    new InternalItemsQuery(user)
                    {
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending) },
                        SeriesPresentationUniqueKey = presentationUniqueKey,
                        Limit = limit,
                        DtoOptions = new DtoOptions { Fields = new[] { ItemFields.SeriesPresentationUniqueKey }, EnableImages = false },
                        GroupBySeriesPresentationUniqueKey = true
                    },
                    parentsFolders.ToList())
                .Cast<Episode>()
                .Where(episode => !string.IsNullOrEmpty(episode.SeriesPresentationUniqueKey))
                .Select(GetUniqueSeriesKey)
                .ToList();

            // Avoid implicitly captured closure
            var episodes = GetNextUpEpisodes(request, user, items, options);

            return GetResult(episodes, request);
        }

        private IEnumerable<Episode> GetNextUpEpisodes(NextUpQuery request, User user, IReadOnlyList<string> seriesKeys, DtoOptions dtoOptions)
        {
            var allNextUp = seriesKeys.Select(i => GetNextUp(i, user, dtoOptions, false));

            if (request.EnableRewatching)
            {
                allNextUp = allNextUp.Concat(
                    seriesKeys.Select(i => GetNextUp(i, user, dtoOptions, true)))
                .OrderByDescending(i => i.LastWatchedDate);
            }

            // If viewing all next up for all series, remove first episodes
            // But if that returns empty, keep those first episodes (avoid completely empty view)
            var alwaysEnableFirstEpisode = !string.IsNullOrEmpty(request.SeriesId);
            var anyFound = false;

            return allNextUp
                .Where(i =>
                {
                    if (request.DisableFirstEpisode)
                    {
                        return i.LastWatchedDate != DateTime.MinValue;
                    }

                    if (alwaysEnableFirstEpisode || (i.LastWatchedDate != DateTime.MinValue && i.LastWatchedDate.Date >= request.NextUpDateCutoff))
                    {
                        anyFound = true;
                        return true;
                    }

                    return !anyFound && i.LastWatchedDate == DateTime.MinValue;
                })
                .Select(i => i.GetEpisodeFunction())
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
        private (DateTime LastWatchedDate, Func<Episode> GetEpisodeFunction) GetNextUp(string seriesKey, User user, DtoOptions dtoOptions, bool rewatching)
        {
            var lastQuery = new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                OrderBy = new[] { (ItemSortBy.ParentIndexNumber, SortOrder.Descending), (ItemSortBy.IndexNumber, SortOrder.Descending) },
                IsPlayed = true,
                Limit = 1,
                ParentIndexNumberNotEquals = 0,
                DtoOptions = new DtoOptions
                {
                    Fields = new[] { ItemFields.SortName },
                    EnableImages = false
                }
            };

            if (rewatching)
            {
                // find last watched by date played, not by newest episode watched
                lastQuery.OrderBy = new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.ParentIndexNumber, SortOrder.Descending), (ItemSortBy.IndexNumber, SortOrder.Descending) };
            }

            var lastWatchedEpisode = _libraryManager.GetItemList(lastQuery).Cast<Episode>().FirstOrDefault();

            Episode GetEpisode()
            {
                var nextQuery = new InternalItemsQuery(user)
                {
                    AncestorWithPresentationUniqueKey = null,
                    SeriesPresentationUniqueKey = seriesKey,
                    IncludeItemTypes = new[] { BaseItemKind.Episode },
                    OrderBy = new[] { (ItemSortBy.ParentIndexNumber, SortOrder.Ascending), (ItemSortBy.IndexNumber, SortOrder.Ascending) },
                    Limit = 1,
                    IsPlayed = rewatching,
                    IsVirtualItem = false,
                    ParentIndexNumberNotEquals = 0,
                    DtoOptions = dtoOptions
                };

                Episode nextEpisode;
                if (rewatching)
                {
                    nextQuery.Limit = 2;
                    // get watched episode after most recently watched
                    nextEpisode = _libraryManager.GetItemList(nextQuery).Cast<Episode>().ElementAtOrDefault(1);
                }
                else
                {
                    nextEpisode = _libraryManager.GetItemList(nextQuery).Cast<Episode>().FirstOrDefault();
                }

                if (_configurationManager.Configuration.DisplaySpecialsWithinSeasons)
                {
                    var consideredEpisodes = _libraryManager.GetItemList(new InternalItemsQuery(user)
                    {
                        AncestorWithPresentationUniqueKey = null,
                        SeriesPresentationUniqueKey = seriesKey,
                        ParentIndexNumber = 0,
                        IncludeItemTypes = new[] { BaseItemKind.Episode },
                        IsPlayed = rewatching,
                        IsVirtualItem = false,
                        DtoOptions = dtoOptions
                    })
                    .Cast<Episode>()
                    .Where(episode => episode.AirsBeforeSeasonNumber != null || episode.AirsAfterSeasonNumber != null)
                    .ToList();

                    if (lastWatchedEpisode != null)
                    {
                        // Last watched episode is added, because there could be specials that aired before the last watched episode
                        consideredEpisodes.Add(lastWatchedEpisode);
                    }

                    if (nextEpisode != null)
                    {
                        consideredEpisodes.Add(nextEpisode);
                    }

                    var sortedConsideredEpisodes = _libraryManager.Sort(consideredEpisodes, user, new[] { (ItemSortBy.AiredEpisodeOrder, SortOrder.Ascending) })
                        .Cast<Episode>();
                    if (lastWatchedEpisode != null)
                    {
                        sortedConsideredEpisodes = sortedConsideredEpisodes.SkipWhile(episode => !episode.Id.Equals(lastWatchedEpisode.Id)).Skip(1);
                    }

                    nextEpisode = sortedConsideredEpisodes.FirstOrDefault();
                }

                if (nextEpisode != null)
                {
                    var userData = _userDataManager.GetUserData(user, nextEpisode);

                    if (userData.PlaybackPositionTicks > 0)
                    {
                        return null;
                    }
                }

                return nextEpisode;
            }

            if (lastWatchedEpisode != null)
            {
                var userData = _userDataManager.GetUserData(user, lastWatchedEpisode);

                var lastWatchedDate = userData.LastPlayedDate ?? DateTime.MinValue.AddDays(1);

                return (lastWatchedDate, GetEpisode);
            }

            // Return the first episode
            return (DateTime.MinValue, GetEpisode);
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

            return new QueryResult<BaseItem>(
                query.StartIndex,
                totalCount,
                items.ToArray());
        }
    }
}
