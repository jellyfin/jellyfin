using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Season
    /// </summary>
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

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            double value = 2;
            value /= 3;

            return value;
        }

        public string FindSeriesSortName()
        {
            var series = Series;
            return series == null ? SeriesName : series.SortName;
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var series = Series;
            if (series != null)
            {
                list.InsertRange(0, series.GetUserDataKeys().Select(i => i + (IndexNumber ?? 0).ToString("000")));
            }

            return list;
        }

        public override int GetChildCount(User user)
        {
            var result = GetChildren(user, true).Count;

            return result;
        }

        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [JsonIgnore]
        public Series Series
        {
            get
            {
                var seriesId = SeriesId;
                if (seriesId == Guid.Empty)
                {
                    seriesId = FindSeriesId();
                }
                return seriesId == Guid.Empty ? null : (LibraryManager.GetItemById(seriesId) as Series);
            }
        }

        [JsonIgnore]
        public string SeriesPath
        {
            get
            {
                var series = Series;

                if (series != null)
                {
                    return series.Path;
                }

                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        public override string CreatePresentationUniqueKey()
        {
            if (IndexNumber.HasValue)
            {
                var series = Series;
                if (series != null)
                {
                    return series.PresentationUniqueKey + "-" + (IndexNumber ?? 0).ToString("000");
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
            return IndexNumber != null ? IndexNumber.Value.ToString("0000") : Name;
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return base.GetItemsInternal(query);
            }

            var user = query.User;

            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

            var items = GetEpisodes(user, query.DtoOptions).Where(filter);

            return PostFilterAndSort(items, query, false);
        }

        /// <summary>
        /// Gets the episodes.
        /// </summary>
        public List<BaseItem> GetEpisodes(User user, DtoOptions options)
        {
            return GetEpisodes(Series, user, options);
        }

        public List<BaseItem> GetEpisodes(Series series, User user, DtoOptions options)
        {
            return GetEpisodes(series, user, null, options);
        }

        public List<BaseItem> GetEpisodes(Series series, User user, IEnumerable<Episode> allSeriesEpisodes, DtoOptions options)
        {
            return series.GetSeasonEpisodes(this, user, allSeriesEpisodes, options);
        }

        public List<BaseItem> GetEpisodes()
        {
            return Series.GetSeasonEpisodes(this, null, null, new DtoOptions(true));
        }

        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            return GetEpisodes(user, new DtoOptions(true));
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block. Let either the entire series rating or episode rating determine it
            return false;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public string FindSeriesPresentationUniqueKey()
        {
            var series = Series;
            return series == null ? null : series.PresentationUniqueKey;
        }

        public string FindSeriesName()
        {
            var series = Series;
            return series == null ? SeriesName : series.Name;
        }

        public Guid FindSeriesId()
        {
            var series = FindParent<Series>();
            return series == null ? Guid.Empty : series.Id;
        }

        /// <summary>
        /// Gets the lookup information.
        /// </summary>
        /// <returns>SeasonInfo.</returns>
        public SeasonInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<SeasonInfo>();

            var series = Series;

            if (series != null)
            {
                id.SeriesProviderIds = series.ProviderIds;
            }

            return id;
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetdata);

            if (!IndexNumber.HasValue && !string.IsNullOrEmpty(Path))
            {
                IndexNumber = IndexNumber ?? LibraryManager.GetSeasonNumberFromPath(Path);

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
