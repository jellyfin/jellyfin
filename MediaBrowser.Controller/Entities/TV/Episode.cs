using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Episode
    /// </summary>
    public class Episode : Video, IHasTrailers, IHasLookupInfo<EpisodeInfo>, IHasSeries
    {
        public Episode()
        {
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            RemoteTrailerIds = new List<Guid>();
        }

        public List<Guid> LocalTrailerIds { get; set; }
        public List<Guid> RemoteTrailerIds { get; set; }
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets the season in which it aired.
        /// </summary>
        /// <value>The aired season.</value>
        public int? AirsBeforeSeasonNumber { get; set; }
        public int? AirsAfterSeasonNumber { get; set; }
        public int? AirsBeforeEpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the DVD season number.
        /// </summary>
        /// <value>The DVD season number.</value>
        public int? DvdSeasonNumber { get; set; }
        /// <summary>
        /// Gets or sets the DVD episode number.
        /// </summary>
        /// <value>The DVD episode number.</value>
        public float? DvdEpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the absolute episode number.
        /// </summary>
        /// <value>The absolute episode number.</value>
        public int? AbsoluteEpisodeNumber { get; set; }

        /// <summary>
        /// This is the ending episode number for double episodes.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumberEnd { get; set; }

        [IgnoreDataMember]
        public string SeriesSortName { get; set; }

        public string FindSeriesSortName()
        {
            var series = Series;
            return series == null ? SeriesSortName : series.SortName;
        }

        [IgnoreDataMember]
        protected override bool SupportsOwnedItems
        {
            get
            {
                return IsStacked || MediaSourceCount > 1;
            }
        }

        [IgnoreDataMember]
        public int? AiredSeasonNumber
        {
            get
            {
                return AirsAfterSeasonNumber ?? AirsBeforeSeasonNumber ?? ParentIndexNumber;
            }
        }

        [IgnoreDataMember]
        public override Folder LatestItemsIndexContainer
        {
            get
            {
                return Series;
            }
        }

        [IgnoreDataMember]
        public override Guid? DisplayParentId
        {
            get
            {
                return SeasonId;
            }
        }

        [IgnoreDataMember]
        protected override bool EnableDefaultVideoUserDataKeys
        {
            get
            {
                return false;
            }
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            var series = Series;
            if (series != null && ParentIndexNumber.HasValue && IndexNumber.HasValue)
            {
                var seriesUserDataKeys = series.GetUserDataKeys();
                var take = seriesUserDataKeys.Count;
                if (seriesUserDataKeys.Count > 1)
                {
                    take--;
                }
                list.InsertRange(0, seriesUserDataKeys.Take(take).Select(i => i + ParentIndexNumber.Value.ToString("000") + IndexNumber.Value.ToString("000")));
            }

            return list;
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
        public Season Season
        {
            get
            {
                var season = FindParent<Season>();

                // Episodes directly in series folder
                if (season == null)
                {
                    var series = Series;

                    if (series != null && ParentIndexNumber.HasValue)
                    {
                        var findNumber = ParentIndexNumber.Value;

                        season = series.Children
                            .OfType<Season>()
                            .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == findNumber);
                    }
                }

                return season;
            }
        }

        [IgnoreDataMember]
        public bool IsInSeasonFolder
        {
            get
            {
                return FindParent<Season>() != null;
            }
        }

        [IgnoreDataMember]
        public string SeriesName { get; set; }

        [IgnoreDataMember]
        public string SeasonName { get; set; }

        public string FindSeasonName()
        {
            var season = Season;
            return season == null ? SeasonName : season.Name;
        }

        public string FindSeriesName()
        {
            var series = Series;
            return series == null ? SeriesName : series.Name;
        }

        public Guid? FindSeasonId()
        {
            var season = Season;
            return season == null ? (Guid?)null : season.Id;
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("000 - ") : "")
                    + (IndexNumber != null ? IndexNumber.Value.ToString("0000 - ") : "") + Name;
        }

        /// <summary>
        /// Determines whether [contains episode number] [the specified number].
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns><c>true</c> if [contains episode number] [the specified number]; otherwise, <c>false</c>.</returns>
        public bool ContainsEpisodeNumber(int number)
        {
            if (IndexNumber.HasValue)
            {
                if (IndexNumberEnd.HasValue)
                {
                    return number >= IndexNumber.Value && number <= IndexNumberEnd.Value;
                }

                return IndexNumber.Value == number;
            }

            return false;
        }

        [IgnoreDataMember]
        public override bool SupportsRemoteImageDownloading
        {
            get
            {
                if (IsMissingEpisode)
                {
                    return false;
                }

                return true;
            }
        }

        [IgnoreDataMember]
        public bool IsMissingEpisode
        {
            get
            {
                return LocationType == LocationType.Virtual && !IsUnaired;
            }
        }

        [IgnoreDataMember]
        public bool IsVirtualUnaired
        {
            get { return LocationType == LocationType.Virtual && IsUnaired; }
        }

        [IgnoreDataMember]
        public Guid? SeasonId { get; set; }
        [IgnoreDataMember]
        public Guid? SeriesId { get; set; }

        public Guid? FindSeriesId()
        {
            var series = Series;
            return series == null ? (Guid?)null : series.Id;
        }

        public override IEnumerable<Guid> GetAncestorIds()
        {
            var list = base.GetAncestorIds().ToList();

            var seasonId = SeasonId;

            if (seasonId.HasValue && !list.Contains(seasonId.Value))
            {
                list.Add(seasonId.Value);
            }

            return list;
        }

        public override IEnumerable<string> GetDeletePaths()
        {
            return new[] { Path };
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Series;
        }

        public EpisodeInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<EpisodeInfo>();

            var series = Series;

            if (series != null)
            {
                id.SeriesProviderIds = series.ProviderIds;
                id.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            id.IsMissingEpisode = IsMissingEpisode;
            id.IndexNumberEnd = IndexNumberEnd;
            id.IsVirtualUnaired = IsVirtualUnaired;

            return id;
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            try
            {
                if (LibraryManager.FillMissingEpisodeNumbersFromPath(this))
                {
                    hasChanges = true;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error in FillMissingEpisodeNumbersFromPath. Episode: {0}", ex, Path ?? Name ?? Id.ToString());
            }

            if (!ParentIndexNumber.HasValue)
            {
                var season = Season;
                if (season != null)
                {
                    if (season.ParentIndexNumber.HasValue)
                    {
                        ParentIndexNumber = season.ParentIndexNumber;
                        hasChanges = true;
                    }
                }
            }

            return hasChanges;
        }
    }
}
