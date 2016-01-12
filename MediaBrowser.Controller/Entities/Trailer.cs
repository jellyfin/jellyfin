using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Controller.Entities.Movies;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    [Obsolete]
    public class Trailer : Video, IHasCriticRating, IHasProductionLocations, IHasBudget, IHasKeywords, IHasTaglines, IHasMetascore, IHasLookupInfo<TrailerInfo>
    {
        public List<string> ProductionLocations { get; set; }

        public Trailer()
        {
            RemoteTrailers = new List<MediaUrl>();
            Taglines = new List<string>();
            Keywords = new List<string>();
            ProductionLocations = new List<string>();
        }

        public float? Metascore { get; set; }

        public List<MediaUrl> RemoteTrailers { get; set; }

        public List<string> Keywords { get; set; }

        /// <summary>
        /// Gets or sets the taglines.
        /// </summary>
        /// <value>The taglines.</value>
        public List<string> Taglines { get; set; }

        /// <summary>
        /// Gets or sets the budget.
        /// </summary>
        /// <value>The budget.</value>
        public double? Budget { get; set; }

        /// <summary>
        /// Gets or sets the revenue.
        /// </summary>
        /// <value>The revenue.</value>
        public double? Revenue { get; set; }

        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        public float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the critic rating summary.
        /// </summary>
        /// <value>The critic rating summary.</value>
        public string CriticRatingSummary { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is local trailer.
        /// </summary>
        /// <value><c>true</c> if this instance is local trailer; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool IsLocalTrailer
        {
            get
            {
                // Local trailers are not part of children
                return GetParent() == null;
            }
        }

        protected override string CreateUserDataKey()
        {
            var key = Movie.GetMovieUserDataKey(this);

            if (!string.IsNullOrWhiteSpace(key))
            {
                key = key + "-trailer";

                // Make sure different trailers have their own data.
                if (RunTimeTicks.HasValue)
                {
                    key += "-" + RunTimeTicks.Value.ToString(CultureInfo.InvariantCulture);
                }

                return key;
            }

            return base.CreateUserDataKey();
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Trailer;
        }

        public TrailerInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<TrailerInfo>();

            info.IsLocalTrailer = IsLocalTrailer;

            return info;
        }
    }
}
