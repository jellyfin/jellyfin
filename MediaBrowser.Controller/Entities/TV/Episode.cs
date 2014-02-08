using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Episode
    /// </summary>
    public class Episode : Video, IHasLookupInfo<EpisodeInfo>, IHasSeries
    {
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

        /// <summary>
        /// We want to group into series not show individually in an index
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public int? AiredSeasonNumber
        {
            get
            {
                return AirsAfterSeasonNumber ?? AirsBeforeSeasonNumber ?? PhysicalSeasonNumber;
            }
        }

        [IgnoreDataMember]
        public int? PhysicalSeasonNumber
        {
            get
            {
                var value = ParentIndexNumber;

                if (value.HasValue)
                {
                    return value;
                }

                var season = Season;

                return season != null ? season.IndexNumber : null;
            }
        }

        /// <summary>
        /// We roll up into series
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return FindParent<Season>();
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            var series = Series;

            if (series != null && ParentIndexNumber.HasValue && IndexNumber.HasValue)
            {
                return series.GetUserDataKey() + ParentIndexNumber.Value.ToString("000") + IndexNumber.Value.ToString("000");
            }

            return base.GetUserDataKey();
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get
            {
                var series = Series;
                return series != null ? series.OfficialRatingForComparison : base.OfficialRatingForComparison;
            }
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
            get { return FindParent<Season>(); }
        }

        [IgnoreDataMember]
        public string SeriesName
        {
            get
            {
                var series = Series;
                return series == null ? null : series.Name;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return (ParentIndexNumber != null ? ParentIndexNumber.Value.ToString("000-") : "")
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
        public bool IsMissingEpisode
        {
            get
            {
                return LocationType == Model.Entities.LocationType.Virtual && PremiereDate.HasValue && PremiereDate.Value < DateTime.UtcNow;
            }
        }

        [IgnoreDataMember]
        public bool IsUnaired
        {
            get { return PremiereDate.HasValue && PremiereDate.Value.ToLocalTime().Date >= DateTime.Now.Date; }
        }

        [IgnoreDataMember]
        public bool IsVirtualUnaired
        {
            get { return LocationType == Model.Entities.LocationType.Virtual && IsUnaired; }
        }

        [IgnoreDataMember]
        public Guid? SeasonId
        {
            get
            {
                // First see if the parent is a Season
                var season = Season;

                if (season != null)
                {
                    return season.Id;
                }

                var seasonNumber = ParentIndexNumber;

                // Parent is a Series
                if (seasonNumber.HasValue)
                {
                    var series = Series;

                    if (series != null)
                    {
                        season = series.Children.OfType<Season>()
                            .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber.Value);

                        if (season != null)
                        {
                            return season.Id;
                        }
                    }
                }

                return null;
            }
        }

        public override IEnumerable<string> GetDeletePaths()
        {
            return new[] { Path };
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedSeries;
        }

        public EpisodeInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<EpisodeInfo>();

            var series = Series;

            if (series != null)
            {
                id.SeriesProviderIds = series.ProviderIds;
            }

            id.IndexNumberEnd = IndexNumberEnd;

            return id;
        }
    }
}
