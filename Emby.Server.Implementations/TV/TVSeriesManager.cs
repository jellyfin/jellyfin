#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
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
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _configurationManager;

        public TVSeriesManager(IUserDataManager userDataManager, ILibraryManager libraryManager, IServerConfigurationManager configurationManager)
        {
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _configurationManager = configurationManager;
        }

        public QueryResult<BaseItem> GetNextUp(NextUpQuery query, DtoOptions options)
        {
            var user = query.User;

            string? presentationUniqueKey = null;
            if (!query.SeriesId.IsNullOrEmpty())
            {
                if (_libraryManager.GetItemById(query.SeriesId.Value) is Series series)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetNextUpBatched(query, user, [presentationUniqueKey], options);
            }

            BaseItem[] parents;

            if (query.ParentId.HasValue)
            {
                var parent = _libraryManager.GetItemById(query.ParentId.Value);

                if (parent is not null)
                {
                    parents = [parent];
                }
                else
                {
                    parents = [];
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
            var user = request.User;

            string? presentationUniqueKey = null;
            int? limit = null;
            if (!request.SeriesId.IsNullOrEmpty())
            {
                if (_libraryManager.GetItemById(request.SeriesId.Value) is Series series)
                {
                    presentationUniqueKey = GetUniqueSeriesKey(series);
                    limit = 1;
                }
            }

            if (!string.IsNullOrEmpty(presentationUniqueKey))
            {
                return GetNextUpBatched(request, user, [presentationUniqueKey], options);
            }

            if (limit.HasValue)
            {
                limit = limit.Value + 10;
            }

            var nextUpSeriesKeys = _libraryManager.GetNextUpSeriesKeys(new InternalItemsQuery(user) { Limit = limit }, parentsFolders, request.NextUpDateCutoff);

            return GetNextUpBatched(request, user, nextUpSeriesKeys, options);
        }

        private QueryResult<BaseItem> GetNextUpBatched(NextUpQuery request, User user, IReadOnlyList<string> seriesKeys, DtoOptions dtoOptions)
        {
            if (seriesKeys.Count == 0)
            {
                return new QueryResult<BaseItem>();
            }

            var includeSpecials = _configurationManager.Configuration.DisplaySpecialsWithinSeasons;
            var includeRewatching = request.EnableRewatching;

            var query = new InternalItemsQuery(user)
            {
                DtoOptions = dtoOptions
            };

            var batchResult = _libraryManager.GetNextUpEpisodesBatch(query, seriesKeys, includeSpecials, includeRewatching);

            var nextUpList = new List<(DateTime LastWatchedDate, Episode Episode)>();

            foreach (var seriesKey in seriesKeys)
            {
                if (!batchResult.TryGetValue(seriesKey, out var result))
                {
                    continue;
                }

                var nextEpisode = DetermineNextEpisode(result, user, includeSpecials, request.EnableResumable, false);

                if (nextEpisode is not null)
                {
                    DateTime lastWatchedDate = DateTime.MinValue;
                    if (result.LastWatched is not null)
                    {
                        var userData = _userDataManager.GetUserData(user, result.LastWatched);
                        lastWatchedDate = userData?.LastPlayedDate ?? DateTime.MinValue.AddDays(1);
                    }

                    nextUpList.Add((lastWatchedDate, nextEpisode));
                }

                if (includeRewatching)
                {
                    var nextPlayedEpisode = DetermineNextEpisodeForRewatching(result, user, includeSpecials);

                    if (nextPlayedEpisode is not null)
                    {
                        DateTime rewatchLastWatchedDate = DateTime.MinValue;
                        if (result.LastWatchedForRewatching is not null)
                        {
                            var userData = _userDataManager.GetUserData(user, result.LastWatchedForRewatching);
                            rewatchLastWatchedDate = userData?.LastPlayedDate ?? DateTime.MinValue.AddDays(1);
                        }

                        nextUpList.Add((rewatchLastWatchedDate, nextPlayedEpisode));
                    }
                }
            }

            var sortedEpisodes = nextUpList
                .OrderByDescending(x => x.LastWatchedDate)
                .Select(x => (BaseItem)x.Episode);

            return GetResult(sortedEpisodes, request);
        }

        private Episode? DetermineNextEpisode(
            MediaBrowser.Controller.Persistence.NextUpEpisodeBatchResult result,
            User user,
            bool includeSpecials,
            bool includeResumable,
            bool includePlayed)
        {
            var nextEpisode = (includePlayed ? result.NextPlayedForRewatching : result.NextUp) as Episode;
            var lastWatchedEpisode = (includePlayed ? result.LastWatchedForRewatching : result.LastWatched) as Episode;

            if (includeSpecials && result.Specials?.Count > 0)
            {
                var consideredEpisodes = result.Specials
                    .Cast<Episode>()
                    .Where(episode => episode.AirsBeforeSeasonNumber is not null || episode.AirsAfterSeasonNumber is not null)
                    .ToList();

                if (lastWatchedEpisode is not null)
                {
                    consideredEpisodes.Add(lastWatchedEpisode);
                }

                if (nextEpisode is not null)
                {
                    consideredEpisodes.Add(nextEpisode);
                }

                if (consideredEpisodes.Count > 0)
                {
                    var sortedEpisodes = _libraryManager.Sort(consideredEpisodes, user, [(ItemSortBy.AiredEpisodeOrder, SortOrder.Ascending)])
                        .Cast<Episode>();

                    if (lastWatchedEpisode is not null)
                    {
                        sortedEpisodes = sortedEpisodes.SkipWhile(episode => !episode.Id.Equals(lastWatchedEpisode.Id)).Skip(1);
                    }

                    nextEpisode = sortedEpisodes.FirstOrDefault();
                }
            }

            if (nextEpisode is not null && !includeResumable)
            {
                var userData = _userDataManager.GetUserData(user, nextEpisode);
                if (userData?.PlaybackPositionTicks > 0)
                {
                    return null;
                }
            }

            return nextEpisode;
        }

        private Episode? DetermineNextEpisodeForRewatching(
            MediaBrowser.Controller.Persistence.NextUpEpisodeBatchResult result,
            User user,
            bool includeSpecials)
        {
            return DetermineNextEpisode(result, user, includeSpecials, includeResumable: false, includePlayed: true);
        }

        private static string GetUniqueSeriesKey(Series series)
        {
            return series.GetPresentationUniqueKey();
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

            if (query.Limit.HasValue && query.Limit.Value > 0)
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
