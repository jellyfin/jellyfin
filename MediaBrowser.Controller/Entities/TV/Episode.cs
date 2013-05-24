using MediaBrowser.Common.Extensions;
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
            if (Series != null)
            {
                var seasonNo = Season != null ? Season.IndexNumber ?? 0 : 0;
                var epNo = IndexNumber ?? 0;
                return Series.GetUserDataKey() + seasonNo.ToString("000") + epNo.ToString("000");
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
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [IgnoreDataMember]
        public override List<string> Studios
        {
            get
            {
                return Series != null ? Series.Studios : null;
            }
            set
            {
                base.Studios = value;
            }
        }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        [IgnoreDataMember]
        public override List<string> Genres
        {
            get { return Series != null ? Series.Genres : null; }
            set
            {
                base.Genres = value;
            }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        public override string OfficialRating
        {
            get { return Series != null ? Series.OfficialRating : base.OfficialRating; }
            set
            {
                base.OfficialRating = value;
            }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        public override string CustomRating
        {
            get { return Series != null ? Series.CustomRating : base.CustomRating; }
            set
            {
                base.CustomRating = value;
            }
        }

        /// <summary>
        /// We persist the MB Id of our series object so we can always find it no matter
        /// what context we happen to be loaded from.
        /// </summary>
        /// <value>The series item id.</value>
        public Guid SeriesItemId { get; set; }

        /// <summary>
        /// We persist the MB Id of our season object so we can always find it no matter
        /// what context we happen to be loaded from.
        /// </summary>
        /// <value>The season item id.</value>
        public Guid SeasonItemId { get; set; }

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
    }
}
