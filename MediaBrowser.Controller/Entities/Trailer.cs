using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    public class Trailer : Video, IHasCriticRating, IHasSoundtracks, IHasBudget
    {
        public List<Guid> SoundtrackIds { get; set; }
        
        public Trailer()
        {
            RemoteTrailers = new List<MediaUrl>();
            Taglines = new List<string>();
            SoundtrackIds = new List<Guid>();
        }

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
                return Parent == null;
            }
        }

        /// <summary>
        /// Should be overridden to return the proper folder where metadata lives
        /// </summary>
        /// <value>The meta location.</value>
        [IgnoreDataMember]
        public override string MetaLocation
        {
            get
            {
                if (!IsLocalTrailer)
                {
                    return System.IO.Path.GetDirectoryName(Path);
                }

                return base.MetaLocation;
            }
        }

        /// <summary>
        /// Needed because the resolver stops at the trailer folder and we find the video inside.
        /// </summary>
        /// <value><c>true</c> if [use parent path to create resolve args]; otherwise, <c>false</c>.</value>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get { return !IsLocalTrailer; }
        }

        public override string GetUserDataKey()
        {
            var key = this.GetProviderId(MetadataProviders.Tmdb) ?? this.GetProviderId(MetadataProviders.Tvdb) ?? this.GetProviderId(MetadataProviders.Imdb) ?? this.GetProviderId(MetadataProviders.Tvcom);

            if (!string.IsNullOrWhiteSpace(key))
            {
                return key + "-trailer";
            }

            return base.GetUserDataKey();
        }
    }
}
