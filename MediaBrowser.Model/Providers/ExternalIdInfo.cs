namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Represents the external id information for serialization to the client.
    /// </summary>
    public class ExternalIdInfo
    {
        /// <summary>
        /// Gets or sets the display name of the external id provider (IE: IMDB, MusicBrainz, etc).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the unique key for this id. This key should be unique across all providers.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the media type (Album, Artist, etc).
        /// This can be null if there is no specific type.
        /// This string is also used to localize the media type on the client.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the URL format string.
        /// </summary>
        public string UrlFormatString { get; set; }
    }
}
