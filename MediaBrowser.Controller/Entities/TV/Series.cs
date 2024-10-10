#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Querying;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Series.
    /// </summary>
    public class Series : Folder, IHasTrailers, IHasDisplayOrder, IHasLookupInfo<SeriesInfo>, IMetadataContainer
    {
        public Series()
        {
            AirDays = Array.Empty<DayOfWeek>();
        }

        public DayOfWeek[] AirDays { get; set; }

        public string AirTime { get; set; }

        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        [JsonIgnore]
        public override bool SupportsDateLastMediaAdded => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        /// <inheritdoc />
        [JsonIgnore]
        public IReadOnlyList<BaseItem> LocalTrailers => GetExtras()
            .Where(extra => extra.ExtraType == Model.Entities.ExtraType.Trailer)
            .ToArray();

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        /// <remarks>
        /// Valid options are airdate, dvd or absolute.
        /// </remarks>
        public string DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public SeriesStatus? Status { get; set; }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            double value = 2;
            value /= 3;

            return value;
        }

        public override string CreatePresentationUniqueKey()
        {
            if (LibraryManager.GetLibraryOptions(this).EnableAutomaticSeriesGrouping)
            {
                var userdatakeys = GetUserDataKeys();

                if (userdatakeys.Count > 1)
                {
                    return AddLibrariesToPresentationUniqueKey(userdatakeys[0]);
                }
            }

            return base.CreatePresentationUniqueKey();
        }

        private string AddLibrariesToPresentationUniqueKey(string key)
        {
            var lang = GetPreferredMetadataLanguage();
            if (!string.IsNullOrEmpty(lang))
            {
                key += "-" + lang;
            }

            var folders = LibraryManager.GetCollectionFolders(this)
                .Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture))
                .ToArray();

            if (folders.Length == 0)
            {
                return key;
            }

            return key + "-" + string.Join('-', folders);
        }

        private static string GetUniqueSeriesKey(BaseItem series)
        {
            return series.GetPresentationUniqueKey();
        }

        public override int GetChildCount(User user)
        {
            var seriesKey = GetUniqueSeriesKey(this);

            var result = LibraryManager.GetCount(new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                IncludeItemTypes = new[] { BaseItemKind.Season },
                IsVirtualItem = false,
                Limit = 0,
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }
            });

            return result;
        }

        public override int GetRecursiveChildCount(User user)
        {
            var seriesKey = GetUniqueSeriesKey(this);

            var query = new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }
            };

            if (query.IncludeItemTypes.Length == 0)
            {
                query.IncludeItemTypes = new[] { BaseItemKind.Episode };
            }

            query.IsVirtualItem = false;
            query.Limit = 0;
            var totalRecordCount = LibraryManager.GetCount(query);

            return totalRecordCount;
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (this.TryGetProviderId(MetadataProvider.Imdb, out var key))
            {
                list.Insert(0, key);
            }

            if (this.TryGetProviderId(MetadataProvider.Tvdb, out key))
            {
                list.Insert(0, key);
            }

            if (this.TryGetProviderId(MetadataProvider.Custom, out key))
            {
                list.Insert(0, key);
            }

            return list;
        }

        public override IReadOnlyList<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            return GetSeasons(user, new DtoOptions(true));
        }

        public IReadOnlyList<BaseItem> GetSeasons(User user, DtoOptions options)
        {
            var query = new InternalItemsQuery(user)
            {
                DtoOptions = options
            };

            SetSeasonQueryOptions(query, user);

            return LibraryManager.GetItemList(query);
        }

        private void SetSeasonQueryOptions(InternalItemsQuery query, User user)
        {
            var seriesKey = GetUniqueSeriesKey(this);

            query.AncestorWithPresentationUniqueKey = null;
            query.SeriesPresentationUniqueKey = seriesKey;
            query.IncludeItemTypes = new[] { BaseItemKind.Season };
            query.OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) };

            if (user is not null && !user.DisplayMissingEpisodes)
            {
                query.IsMissing = false;
            }
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            var user = query.User;

            if (query.Recursive)
            {
                var seriesKey = GetUniqueSeriesKey(this);

                query.AncestorWithPresentationUniqueKey = null;
                query.SeriesPresentationUniqueKey = seriesKey;
                if (query.OrderBy.Count == 0)
                {
                    query.OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) };
                }

                if (query.IncludeItemTypes.Length == 0)
                {
                    query.IncludeItemTypes = new[] { BaseItemKind.Episode, BaseItemKind.Season };
                }

                query.IsVirtualItem = false;
                return LibraryManager.GetItemsResult(query);
            }

            SetSeasonQueryOptions(query, user);

            return LibraryManager.GetItemsResult(query);
        }

        public IEnumerable<BaseItem> GetEpisodes(User user, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            var seriesKey = GetUniqueSeriesKey(this);

            var query = new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = null,
                SeriesPresentationUniqueKey = seriesKey,
                IncludeItemTypes = new[] { BaseItemKind.Episode, BaseItemKind.Season },
                OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                DtoOptions = options,
            };

            if (!shouldIncludeMissingEpisodes)
            {
                query.IsMissing = false;
            }

            var allItems = LibraryManager.GetItemList(query);

            var allSeriesEpisodes = allItems.OfType<Episode>().ToList();

            var allEpisodes = allItems.OfType<Season>()
                .SelectMany(i => i.GetEpisodes(this, user, allSeriesEpisodes, options, shouldIncludeMissingEpisodes))
                .Reverse();

            // Specials could appear twice based on above - once in season 0, once in the aired season
            // This depends on settings for that series
            // When this happens, remove the duplicate from season 0

            return allEpisodes.DistinctBy(i => i.Id).Reverse();
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Refresh bottom up, seasons and episodes first, then the series
            var items = GetRecursiveChildren();

            var totalItems = items.Count;
            var numComplete = 0;

            // Refresh seasons
            foreach (var item in items)
            {
                if (item is not Season)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (refreshOptions.RefreshItem(item))
                {
                    await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                }

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            // Refresh episodes and other children
            foreach (var item in items)
            {
                if (item is Season)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                bool skipItem = item is Episode episode
                    && refreshOptions.MetadataRefreshMode != MetadataRefreshMode.FullRefresh
                    && !refreshOptions.ReplaceAllMetadata
                    && episode.IsMissingEpisode
                    && episode.LocationType == LocationType.Virtual
                    && episode.PremiereDate.HasValue
                    && (DateTime.UtcNow - episode.PremiereDate.Value).TotalDays > 30;

                if (!skipItem)
                {
                    if (refreshOptions.RefreshItem(item))
                    {
                        await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            refreshOptions = new MetadataRefreshOptions(refreshOptions);
            await ProviderManager.RefreshSingleItem(this, refreshOptions, cancellationToken).ConfigureAwait(false);
        }

        public List<BaseItem> GetSeasonEpisodes(Season parentSeason, User user, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            var queryFromSeries = ConfigurationManager.Configuration.DisplaySpecialsWithinSeasons;

            // add optimization when this setting is not enabled
            var seriesKey = queryFromSeries ?
                GetUniqueSeriesKey(this) :
                GetUniqueSeriesKey(parentSeason);

            var query = new InternalItemsQuery(user)
            {
                AncestorWithPresentationUniqueKey = queryFromSeries ? null : seriesKey,
                SeriesPresentationUniqueKey = queryFromSeries ? seriesKey : null,
                IncludeItemTypes = new[] { BaseItemKind.Episode },
                OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                DtoOptions = options
            };

            if (!shouldIncludeMissingEpisodes)
            {
                query.IsMissing = false;
            }

            var allItems = LibraryManager.GetItemList(query);

            return GetSeasonEpisodes(parentSeason, user, allItems, options, shouldIncludeMissingEpisodes);
        }

        public List<BaseItem> GetSeasonEpisodes(Season parentSeason, User user, IEnumerable<BaseItem> allSeriesEpisodes, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            if (allSeriesEpisodes is null)
            {
                return GetSeasonEpisodes(parentSeason, user, options, shouldIncludeMissingEpisodes);
            }

            var episodes = FilterEpisodesBySeason(allSeriesEpisodes, parentSeason, ConfigurationManager.Configuration.DisplaySpecialsWithinSeasons);

            var sortBy = (parentSeason.IndexNumber ?? -1) == 0 ? ItemSortBy.SortName : ItemSortBy.AiredEpisodeOrder;

            return LibraryManager.Sort(episodes, user, new[] { sortBy }, SortOrder.Ascending).ToList();
        }

        /// <summary>
        /// Filters the episodes by season.
        /// </summary>
        /// <param name="episodes">The episodes.</param>
        /// <param name="parentSeason">The season.</param>
        /// <param name="includeSpecials"><c>true</c> to include special, <c>false</c> to not.</param>
        /// <returns>The set of episodes.</returns>
        public static IEnumerable<BaseItem> FilterEpisodesBySeason(IEnumerable<BaseItem> episodes, Season parentSeason, bool includeSpecials)
        {
            var seasonNumber = parentSeason.IndexNumber;
            var seasonPresentationKey = GetUniqueSeriesKey(parentSeason);

            var supportSpecialsInSeason = includeSpecials && seasonNumber.HasValue && seasonNumber.Value != 0;

            return episodes.Where(episode =>
            {
                var episodeItem = (Episode)episode;

                var currentSeasonNumber = supportSpecialsInSeason ? episodeItem.AiredSeasonNumber : episode.ParentIndexNumber;
                if (currentSeasonNumber.HasValue && seasonNumber.HasValue && currentSeasonNumber.Value == seasonNumber.Value)
                {
                    return true;
                }

                if (!currentSeasonNumber.HasValue && !seasonNumber.HasValue && parentSeason.LocationType == LocationType.Virtual)
                {
                    return true;
                }

                var season = episodeItem.Season;
                return season is not null && string.Equals(GetUniqueSeriesKey(season), seasonPresentationKey, StringComparison.OrdinalIgnoreCase);
            });
        }

        /// <summary>
        /// Filters the episodes by season.
        /// </summary>
        /// <param name="episodes">The episodes.</param>
        /// <param name="seasonNumber">The season.</param>
        /// <param name="includeSpecials"><c>true</c> to include special, <c>false</c> to not.</param>
        /// <returns>The set of episodes.</returns>
        public static IEnumerable<Episode> FilterEpisodesBySeason(IEnumerable<Episode> episodes, int seasonNumber, bool includeSpecials)
        {
            if (!includeSpecials || seasonNumber < 1)
            {
                return episodes.Where(i => (i.ParentIndexNumber ?? -1) == seasonNumber);
            }

            return episodes.Where(i =>
            {
                var episode = i;

                if (episode is not null)
                {
                    var currentSeasonNumber = episode.AiredSeasonNumber;

                    return currentSeasonNumber.HasValue && currentSeasonNumber.Value == seasonNumber;
                }

                return false;
            });
        }

        protected override bool GetBlockUnratedValue(User user)
        {
            return user.GetPreferenceValues<UnratedItem>(PreferenceKind.BlockUnratedItems).Contains(UnratedItem.Series);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

        public SeriesInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<SeriesInfo>();

            return info;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

            if (!ProductionYear.HasValue)
            {
                var info = LibraryManager.ParseName(Name);

                var yearInName = info.Year;

                if (yearInName.HasValue)
                {
                    ProductionYear = yearInName;
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
