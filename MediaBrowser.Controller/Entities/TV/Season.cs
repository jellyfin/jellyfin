#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Season.
    /// </summary>
    [RequiresSourceSerialisation]
    public class Season : Folder, IHasSeries, IHasLookupInfo<SeasonInfo>
    {
        [JsonIgnore]
        public override bool SupportsAddingToPlaylist => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        [JsonIgnore]
        public override bool SupportsDateLastMediaAdded => false;

        [JsonIgnore]
        public override bool SupportsPeople => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => true;

        [JsonIgnore]
        public override Guid DisplayParentId => SeriesId;

        /// <summary>
        /// Gets this Episode's Series Instance.
        /// </summary>
        /// <value>The series.</value>
        [JsonIgnore]
        public Series Series
        {
            get
            {
                var seriesId = SeriesId;
                if (seriesId.IsEmpty())
                {
                    seriesId = FindSeriesId();
                }

                return seriesId.IsEmpty() ? null : (LibraryManager.GetItemById(seriesId) as Series);
            }
        }

        [JsonIgnore]
        public string SeriesPath
        {
            get
            {
                var series = Series;

                if (series is not null)
                {
                    return series.Path;
                }

                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            double value = 2;
            value /= 3;

            return value;
        }

        public string FindSeriesSortName()
        {
            var series = Series;
            return series is null ? SeriesName : series.SortName;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var series = Series;
            if (series is not null)
            {
                var newList = series.GetUserDataKeys();
                var suffix = (IndexNumber ?? 0).ToString("000", CultureInfo.InvariantCulture);
                for (int i = 0; i < newList.Count; i++)
                {
                    newList[i] = newList[i] + suffix;
                }

                newList.AddRange(list);
                list = newList;
            }

            return list;
        }

        public override int GetChildCount(User user)
        {
            var result = GetChildren(user, true).Count;

            return result;
        }

        public override string CreatePresentationUniqueKey()
        {
            if (IndexNumber.HasValue)
            {
                var series = Series;
                if (series is not null)
                {
                    return series.PresentationUniqueKey + "-" + IndexNumber.Value.ToString("000", CultureInfo.InvariantCulture);
                }
            }

            return base.CreatePresentationUniqueKey();
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber is not null ? IndexNumber.Value.ToString("0000", CultureInfo.InvariantCulture) : Name;
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.User is null)
            {
                return base.GetItemsInternal(query);
            }

            var user = query.User;

            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

            var items = GetEpisodes(user, query.DtoOptions, true).Where(filter);

            return PostFilterAndSort(items, query, false);
        }

        /// <summary>
        /// Gets the episodes.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options to use.</param>
        /// <param name="shouldIncludeMissingEpisodes">If missing episodes should be included.</param>
        /// <returns>Set of episodes.</returns>
        public List<BaseItem> GetEpisodes(User user, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            return GetEpisodes(Series, user, options, shouldIncludeMissingEpisodes);
        }

        public List<BaseItem> GetEpisodes(Series series, User user, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            return GetEpisodes(series, user, null, options, shouldIncludeMissingEpisodes);
        }

        public List<BaseItem> GetEpisodes(Series series, User user, IEnumerable<Episode> allSeriesEpisodes, DtoOptions options, bool shouldIncludeMissingEpisodes)
        {
            return series.GetSeasonEpisodes(this, user, allSeriesEpisodes, options, shouldIncludeMissingEpisodes);
        }

        public List<BaseItem> GetEpisodes()
        {
            return Series.GetSeasonEpisodes(this, null, null, new DtoOptions(true), true);
        }

        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            return GetEpisodes(user, new DtoOptions(true), true);
        }

        protected override bool GetBlockUnratedValue(User user)
        {
            // Don't block. Let either the entire series rating or episode rating determine it
            return false;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

        public string FindSeriesPresentationUniqueKey()
        {
            var series = Series;
            return series is null ? null : series.PresentationUniqueKey;
        }

        public string FindSeriesName()
        {
            var series = Series;
            return series is null ? SeriesName : series.Name;
        }

        public Guid FindSeriesId()
        {
            var series = FindParent<Series>();
            return series?.Id ?? Guid.Empty;
        }

        /// <summary>
        /// Gets the lookup information.
        /// </summary>
        /// <returns>SeasonInfo.</returns>
        public SeasonInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<SeasonInfo>();

            var series = Series;

            if (series is not null)
            {
                id.SeriesProviderIds = series.ProviderIds;
                id.SeriesDisplayOrder = series.DisplayOrder;
            }

            return id;
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made.
        /// </summary>
        /// <param name="replaceAllMetadata"><c>true</c> to replace metadata, <c>false</c> to not.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

            if (!IndexNumber.HasValue && !string.IsNullOrEmpty(Path))
            {
                IndexNumber ??= LibraryManager.GetSeasonNumberFromPath(Path);

                // If a change was made record it
                if (IndexNumber.HasValue)
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
