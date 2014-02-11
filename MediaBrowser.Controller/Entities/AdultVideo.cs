using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class AdultVideo : Video, IHasPreferredMetadataLanguage, IHasTaglines
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

        public List<string> Taglines { get; set; }

        public AdultVideo()
        {
            Taglines = new List<string>();
        }
    }
}
