#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Enum MetadataPluginType.
    /// </summary>
    public enum MetadataPluginType
    {
        /// <summary>
        /// Local image provider
        /// </summary>
        LocalImageProvider,

        /// <summary>
        /// Image fetcher
        /// </summary>
        ImageFetcher,

        /// <summary>
        /// Image saver
        /// </summary>
        ImageSaver,

        /// <summary>
        /// Local metadata provider
        /// </summary>
        LocalMetadataProvider,

        /// <summary>
        /// Metadata fetcher
        /// </summary>
        MetadataFetcher,

        /// <summary>
        /// Metadata saver
        /// </summary>
        MetadataSaver,

        /// <summary>
        /// Subtitle fetcher
        /// </summary>
        SubtitleFetcher
    }
}
