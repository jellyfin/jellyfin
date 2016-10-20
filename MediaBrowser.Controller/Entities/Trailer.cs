using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    public class Trailer : Video, IHasBudget, IHasMetascore, IHasOriginalTitle, IHasLookupInfo<TrailerInfo>
    {
        public Trailer()
        {
            RemoteTrailers = new List<MediaUrl>();
            Keywords = new List<string>();
            TrailerTypes = new List<TrailerType> { TrailerType.LocalTrailer };
        }

        public List<TrailerType> TrailerTypes { get; set; }

        public float? Metascore { get; set; }

        public List<MediaUrl> RemoteTrailers { get; set; }

        [IgnoreDataMember]
        public bool IsLocalTrailer
        {
            get { return TrailerTypes.Contains(TrailerType.LocalTrailer); }
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

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Trailer;
        }

        public TrailerInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<TrailerInfo>();

            info.IsLocalTrailer = TrailerTypes.Contains(TrailerType.LocalTrailer);

            if (!DetectIsInMixedFolder() && LocationType == LocationType.FileSystem)
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
                    if (!DetectIsInMixedFolder())
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

        public override List<ExternalUrl> GetRelatedUrls()
        {
            var list = base.GetRelatedUrls();

            var imdbId = this.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                list.Add(new ExternalUrl
                {
                    Name = "Trakt",
                    Url = string.Format("https://trakt.tv/movies/{0}", imdbId)
                });
            }

            return list;
        }

        [IgnoreDataMember]
        public override bool StopRefreshIfLocalMetadataFound
        {
            get
            {
                // Need people id's from internet metadata
                return false;
            }
        }
    }
}
