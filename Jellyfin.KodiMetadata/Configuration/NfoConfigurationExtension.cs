#pragma warning disable CS1591

using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.KodiMetadata.Configuration
{
    public static class NfoConfigurationExtension
    {
        public static XbmcMetadataOptions GetNfoConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");
        }
    }
}
