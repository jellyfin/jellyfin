using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Trailer
    /// </summary>
    public class Trailer : Video, IHasLookupInfo<TrailerInfo>
    {
        public Trailer()
        {
            TrailerTypes = Array.Empty<TrailerType>();
        }

        public TrailerType[] TrailerTypes { get; set; }

        public override double GetDefaultPrimaryImageAspectRatio()
            => 2.0 / 3;

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Trailer;
        }

        public TrailerInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<TrailerInfo>();

            if (!IsInMixedFolder && IsFileProtocol)
            {
                info.Name = System.IO.Path.GetFileName(ContainingFolderPath);
            }

            return info;
        }

        public override bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetdata);

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

        public override List<ExternalUrl> GetRelatedUrls()
        {
            var list = base.GetRelatedUrls();

            var imdbId = this.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrEmpty(imdbId))
            {
                list.Add(new ExternalUrl
                {
                    Name = "Trakt",
                    Url = string.Format("https://trakt.tv/movies/{0}", imdbId)
                });
            }

            return list;
        }

        [JsonIgnore]
        public override bool StopRefreshIfLocalMetadataFound => false;
    }
}
