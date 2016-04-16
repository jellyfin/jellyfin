using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using MediaBrowser.Controller.Entities.Movies;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    public class Trailer : Video, IHasCriticRating, IHasProductionLocations, IHasBudget, IHasKeywords, IHasTaglines, IHasMetascore, IHasLookupInfo<TrailerInfo>
    {
        public List<string> ProductionLocations { get; set; }

        public Trailer()
        {
            RemoteTrailers = new List<MediaUrl>();
            Taglines = new List<string>();
            Keywords = new List<string>();
            ProductionLocations = new List<string>();
            TrailerTypes = new List<TrailerType> { TrailerType.LocalTrailer };
        }

        public List<TrailerType> TrailerTypes { get; set; }

        public float? Metascore { get; set; }

        public List<MediaUrl> RemoteTrailers { get; set; }

        public List<string> Keywords { get; set; }

        [IgnoreDataMember]
        public bool IsLocalTrailer
        {
            get { return TrailerTypes.Contains(TrailerType.LocalTrailer); }
        }

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

            info.IsLocalTrailer = TrailerTypes.Contains(TrailerType.LocalTrailer);

            if (!IsInMixedFolder)
            {
                info.Name = System.IO.Path.GetFileName(ContainingFolderPath);
            }

            return info;
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (!ProductionYear.HasValue)
            {
                var info = LibraryManager.ParseName(Name);

                var yearInName = info.Year;

                if (yearInName.HasValue)
                {
                    ProductionYear = yearInName;
                    hasChanges = true;
                }
                else
                {
                    // Try to get the year from the folder name
                    if (!IsInMixedFolder)
                    {
                        info = LibraryManager.ParseName(System.IO.Path.GetFileName(ContainingFolderPath));

                        yearInName = info.Year;

                        if (yearInName.HasValue)
                        {
                            ProductionYear = yearInName;
                            hasChanges = true;
                        }
                    }
                }
            }

            return hasChanges;
        }
    }
}
