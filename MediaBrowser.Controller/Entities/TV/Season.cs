using System;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Season
    /// </summary>
    public class Season : Folder, IHasSeries, IHasLookupInfo<SeasonInfo>
    {
        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsDateLastMediaAdded
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override Guid? DisplayParentId
        {
            get
            {
                var series = Series;
                return series == null ? ParentId : series.Id;
            }
        }

        [IgnoreDataMember]
        public string SeriesSortName { get; set; }

        public string FindSeriesSortName()
        {
            var series = Series;
            return series == null ? SeriesSortName : series.SortName;
        }

        // Genre, Rating and Stuido will all be the same
        protected override IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {
                {"None"},
                {"Performer"},
                {"Director"},
                {"Year"},
            };
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
            return GetChildren(user, true).Count();
        }

        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [IgnoreDataMember]
        public Series Series
        {
            get { return FindParent<Series>(); }
        }

        [IgnoreDataMember]
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

        [IgnoreDataMember]
        public override string PresentationUniqueKey
        {
            get
            {
                if (IndexNumber.HasValue)
                {
                    var series = Series;
                    if (series != null)
                    {
                        return series.PresentationUniqueKey + "-" + (IndexNumber ?? 0).ToString("000");
                    }
                }

                return base.PresentationUniqueKey;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber != null ? IndexNumber.Value.ToString("0000") : Name;
        }

        [IgnoreDataMember]
        public bool IsMissingSeason
        {
            get { return (IsVirtualItem) && !IsUnaired; }
        }

        [IgnoreDataMember]
        public bool IsVirtualUnaired
        {
            get { return (IsVirtualItem) && IsUnaired; }
        }

        [IgnoreDataMember]
        public bool IsSpecialSeason
        {
            get { return (IndexNumber ?? -1) == 0; }
        }

        protected override Task<QueryResult<BaseItem>> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return base.GetItemsInternal(query);
            }

            var user = query.User;

            Func<BaseItem, bool> filter = i => UserViewBuilder.Filter(i, user, query, UserDataManager, LibraryManager);

            var items = GetEpisodes(user).Where(filter);

            var result = PostFilterAndSort(items, query);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Gets the episodes.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Episode}.</returns>
        public IEnumerable<Episode> GetEpisodes(User user)
        {
            var config = user.Configuration;

            return GetEpisodes(Series, user, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes(Series series, User user, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes)
        {
            return GetEpisodes(series, user, includeMissingEpisodes, includeVirtualUnairedEpisodes, null);
        }

        public IEnumerable<Episode> GetEpisodes(Series series, User user, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes, IEnumerable<Episode> allSeriesEpisodes)
        {
            return series.GetEpisodes(user, this, includeMissingEpisodes, includeVirtualUnairedEpisodes, allSeriesEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes()
        {
            var episodes = GetRecursiveChildren().OfType<Episode>();
            var series = Series;

            if (series != null && series.ContainsEpisodesWithoutSeasonFolders)
            {
                var seasonNumber = IndexNumber;
                var list = episodes.ToList();

                if (seasonNumber.HasValue)
                {
                    list.AddRange(series.GetRecursiveChildren().OfType<Episode>()
                        .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == seasonNumber.Value));
                }
                else
                {
                    list.AddRange(series.GetRecursiveChildren().OfType<Episode>()
                        .Where(i => !i.ParentIndexNumber.HasValue));
                }

                episodes = list.DistinctBy(i => i.Id);
            }

            return episodes;
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetEpisodes(user);
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

        [IgnoreDataMember]
        public string SeriesName { get; set; }

        [IgnoreDataMember]
        public Guid? SeriesId { get; set; }

        public string FindSeriesName()
        {
            var series = Series;
            return series == null ? SeriesName : series.Name;
        }

        public Guid? FindSeriesId()
        {
            var series = Series;
            return series == null ? (Guid?)null : series.Id;
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
                id.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            return id;
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

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
