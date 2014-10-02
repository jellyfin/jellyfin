using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    [Obsolete]
    public class AdultVideo : Video, IHasProductionLocations, IHasPreferredMetadataLanguage, IHasTaglines
    {
        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }
        public List<string> ProductionLocations { get; set; }

        public List<string> Taglines { get; set; }

        public AdultVideo()
        {
            Taglines = new List<string>();
            ProductionLocations = new List<string>();
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (!ProductionYear.HasValue)
            {
                int? yearInName = null;
                string name;

                NameParser.ParseName(Name, out name, out yearInName);

                if (yearInName.HasValue)
                {
                    ProductionYear = yearInName;
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
