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
                return GetResult(GetNextUpEpisodes(query, user, new[] { presentationUniqueKey }, options), query);
            }

            BaseItem[] parents;

            if (query.ParentId.HasValue)
            {
                var parent = _libraryManager.GetItemById(query.ParentId.Value);

                if (parent is not null)
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
                return GetResult(GetNextUpEpisodes(request, user, [presentationUniqueKey], options), request);
            }

            if (limit.HasValue)
            {
                limit = limit.Value + 10;
            }

            var nextUpSeriesKeys = _libraryManager.GetNextUpSeriesKeys(new InternalItemsQuery(user) { Limit = limit }, parentsFolders, request.NextUpDateCutoff);

            var episodes = GetNextUpEpisodes(request, user, nextUpSeriesKeys, options);

            return GetResult(episodes, request);
        }

        private IEnumerable<Episode> GetNextUpEpisodes(NextUpQuery request, User user, IReadOnlyList<string> seriesKeys, DtoOptions dtoOptions)
        {
            var allNextUp = seriesKeys.Select(i => GetNextUp(i, user, dtoOptions, request.EnableResumable, false));

            if (request.EnableRewatching)
            {
                allNextUp = allNextUp
                    .Concat(seriesKeys.Select(i => GetNextUp(i, user, dtoOptions, false, true)))
                    .OrderByDescending(i => i.LastWatchedDate);
            }

            return allNextUp
                .Select(i => i.GetEpisodeFunction())
                .Where(i => i is not null)!;
        }

        private static string GetUniqueSeriesKey(Series series)
        {
            return series.GetPresentationUniqueKey();
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <returns>Task{Episode}.</returns>
        private (DateTime LastWatchedDate, Func<Episode?> GetEpisodeFunction) GetNextUp(string seriesKey, User user, DtoOptions dtoOptions, bool includeResumable, bool includePlayed)
        {
            var lastQuery = new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                IncludeItemTypes = [BaseItemKind.Episode],
                IsPlayed = true,
                Limit = 1,
                ParentIndexNumberNotEquals = 0,
                DtoOptions = new DtoOptions
                {
                    Fields = [ItemFields.SortName],
                    EnableImages = false
                }
            };

            // If including played results, sort first by date played and then by season and episode numbers
            lastQuery.OrderBy = includePlayed
                ? new[] { (ItemSortBy.DatePlayed, SortOrder.Descending), (ItemSortBy.ParentIndexNumber, SortOrder.Descending), (ItemSortBy.IndexNumber, SortOrder.Descending) }
                : new[] { (ItemSortBy.ParentIndexNumber, SortOrder.Descending), (ItemSortBy.IndexNumber, SortOrder.Descending) };

            var lastWatchedEpisode = _libraryManager.GetItemList(lastQuery).Cast<Episode>().FirstOrDefault();

            Episode? GetEpisode()
            {
                var nextQuery = new InternalItemsQuery(user)
                {
                    AncestorWithPresentationUniqueKey = null,
                    SeriesPresentationUniqueKey = seriesKey,
                    IncludeItemTypes = [BaseItemKind.Episode],
                    OrderBy = [(ItemSortBy.ParentIndexNumber, SortOrder.Ascending), (ItemSortBy.IndexNumber, SortOrder.Ascending)],
                    Limit = 1,
                    IsPlayed = includePlayed,
                    IsVirtualItem = false,
                    ParentIndexNumberNotEquals = 0,
                    DtoOptions = dtoOptions
                };

                // Locate the next up episode based on the last watched episode's season and episode number
                var lastWatchedParentIndexNumber = lastWatchedEpisode?.ParentIndexNumber;
                var lastWatchedIndexNumber = lastWatchedEpisode?.IndexNumberEnd ?? lastWatchedEpisode?.IndexNumber;
                if (lastWatchedParentIndexNumber.HasValue && lastWatchedIndexNumber.HasValue)
                {
                    nextQuery.MinParentAndIndexNumber = (lastWatchedParentIndexNumber.Value, lastWatchedIndexNumber.Value + 1);
                }

                var nextEpisode = _libraryManager.GetItemList(nextQuery).Cast<Episode>().FirstOrDefault();

                if (_configurationManager.Configuration.DisplaySpecialsWithinSeasons)
                {
                    var consideredEpisodes = _libraryManager.GetItemList(new InternalItemsQuery(user)
                    {
                        AncestorWithPresentationUniqueKey = null,
                        SeriesPresentationUniqueKey = seriesKey,
                        ParentIndexNumber = 0,
                        IncludeItemTypes = [BaseItemKind.Episode],
                        IsPlayed = includePlayed,
                        IsVirtualItem = false,
                        DtoOptions = dtoOptions
                    })
                    .Cast<Episode>()
                    .Where(episode => episode.AirsBeforeSeasonNumber is not null || episode.AirsAfterSeasonNumber is not null)
                    .ToList();

                    if (lastWatchedEpisode is not null)
                    {
                        // Last watched episode is added, because there could be specials that aired before the last watched episode
                        consideredEpisodes.Add(lastWatchedEpisode);
                    }

                    if (nextEpisode is not null)
                    {
                        consideredEpisodes.Add(nextEpisode);
                    }

                    var sortedConsideredEpisodes = _libraryManager.Sort(consideredEpisodes, user, [(ItemSortBy.AiredEpisodeOrder, SortOrder.Ascending)])
                        .Cast<Episode>();
                    if (lastWatchedEpisode is not null)
                    {
                        sortedConsideredEpisodes = sortedConsideredEpisodes.SkipWhile(episode => !episode.Id.Equals(lastWatchedEpisode.Id)).Skip(1);
                    }

                    nextEpisode = sortedConsideredEpisodes.FirstOrDefault();
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

            if (lastWatchedEpisode is not null)
            {
                var userData = _userDataManager.GetUserData(user, lastWatchedEpisode);

                if (userData is null)
                {
                    return (DateTime.MinValue, GetEpisode);
                }

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
