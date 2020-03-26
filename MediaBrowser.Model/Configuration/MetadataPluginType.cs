#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Enum MetadataPluginType
    /// </summary>
    public enum MetadataPluginType
    {
        LocalImageProvider,
        ImageFetcher,
        ImageSaver,
        LocalMetadataProvider,
        MetadataFetcher,
        MetadataSaver,
        SubtitleFetcher
    }
}
