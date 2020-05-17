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
        /// Gets or sets the specific media type for this id.
        /// </summary>
        /// <remarks>
        /// This can be used along with the <see cref="Name"/> to localize the external id on the client.
        /// </remarks>
        public ExternalIdMediaType Type { get; set; }

        /// <summary>
        /// Gets or sets the URL format string.
        /// </summary>
        public string UrlFormatString { get; set; }
    }
}
