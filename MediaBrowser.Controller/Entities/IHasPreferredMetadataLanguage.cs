
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasPreferredMetadataLanguage
    /// </summary>
    public interface IHasPreferredMetadataLanguage
    {
        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        string PreferredMetadataCountryCode { get; set; }
    }
}
