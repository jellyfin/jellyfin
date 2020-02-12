#pragma warning disable CS1591
#pragma warning disable SA1600

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
