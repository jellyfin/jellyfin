using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Episode
    /// </summary>
    public class Episode : Video
    {
        /// <summary>
        /// Episodes have a special Metadata folder
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                return System.IO.Path.Combine(Parent.Path, "metadata");
            }
        }

        [IgnoreDataMember]
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// We want to group into series not show individually in an index
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get { return true; }
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
                return Season;
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            if (Series != null && ParentIndexNumber.HasValue && IndexNumber.HasValue)
            {
                return Series.GetUserDataKey() + ParentIndexNumber.Value.ToString("000") + IndexNumber.Value.ToString("000");
            }

            return base.GetUserDataKey();
        }

        /// <summary>
        /// Override this if you need to combine/collapse person information
        /// </summary>
        /// <value>All people.</value>
        [IgnoreDataMember]
        public override IEnumerable<PersonInfo> AllPeople
        {
            get
            {
                if (People == null) return Series != null ? Series.People : People;
                return Series != null && Series.People != null ? People.Concat(Series.People) : base.AllPeople;
            }
        }

        /// <summary>
        /// Gets all genres.
        /// </summary>
        /// <value>All genres.</value>
        [IgnoreDataMember]
        public override IEnumerable<string> AllGenres
        {
            get
            {
                if (Genres == null) return Series != null ? Series.Genres : Genres;
                return Series != null && Series.Genres != null ? Genres.Concat(Series.Genres) : base.AllGenres;
            }
        }

        /// <summary>
        /// Gets all studios.
        /// </summary>
        /// <value>All studios.</value>
        [IgnoreDataMember]
        public override IEnumerable<string> AllStudios
        {
            get
            {
                if (Studios == null) return Series != null ? Series.Studios : Studios;
                return Series != null && Series.Studios != null ? Studios.Concat(Series.Studios) : base.AllStudios;
            }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get { return Series != null ? Series.OfficialRatingForComparison : base.OfficialRatingForComparison; }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string CustomRatingForComparison
        {
            get { return Series != null ? Series.CustomRatingForComparison : base.CustomRatingForComparison; }
        }

        /// <summary>
        /// The _series
        /// </summary>
        private Series _series;
        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [IgnoreDataMember]
        public Series Series
        {
            get { return _series ?? (_series = FindParent<Series>()); }
        }

        /// <summary>
        /// The _season
        /// </summary>
        private Season _season;
        /// <summary>
        /// This Episode's Season Instance
        /// </summary>
        /// <value>The season.</value>
        [IgnoreDataMember]
        public Season Season
        {
            get { return _season ?? (_season = FindParent<Season>()); }
        }

        /// <summary>
        /// This is the ending episode number for double episodes.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumberEnd { get; set; }

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
    }
}
