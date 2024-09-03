using System;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Represents the external id information for serialization to the client.
    /// </summary>
    public class ExternalIdInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalIdInfo"/> class.
        /// </summary>
        /// <param name="name">Name of the external id provider (IE: IMDB, MusicBrainz, etc).</param>
        /// <param name="key">Key for this id. This key should be unique across all providers.</param>
        /// <param name="type">Specific media type for this id.</param>
        /// <param name="urlFormatString">URL format string.</param>
        public ExternalIdInfo(string name, string key, ExternalIdMediaType? type, string? urlFormatString)
        {
            Name = name;
            Key = key;
            Type = type;
#pragma warning disable CS0618 // Type or member is obsolete - Remove 10.11
            UrlFormatString = urlFormatString;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Gets or sets the display name of the external id provider (IE: IMDB, MusicBrainz, etc).
        /// </summary>
        // TODO: This should be renamed to ProviderName
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the unique key for this id. This key should be unique across all providers.
        /// </summary>
        // TODO: This property is not actually unique across the concrete types at the moment. It should be updated to be unique.
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the specific media type for this id. This is used to distinguish between the different
        /// external id types for providers with multiple ids.
        /// A null value indicates there is no specific media type associated with the external id, or this is the
        /// default id for the external provider so there is no need to specify a type.
        /// </summary>
        /// <remarks>
        /// This can be used along with the <see cref="Name"/> to localize the external id on the client.
        /// </remarks>
        public ExternalIdMediaType? Type { get; set; }

        /// <summary>
        /// Gets or sets the URL format string.
        /// </summary>
        [Obsolete("Obsolete in 10.10, to be removed in 10.11")]
        public string? UrlFormatString { get; set; }
    }
}
