
namespace MediaBrowser.Controller.Entities
{
    public class AdultVideo : Video, IHasPreferredMetadataLanguage
    {
        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }
    }
}
