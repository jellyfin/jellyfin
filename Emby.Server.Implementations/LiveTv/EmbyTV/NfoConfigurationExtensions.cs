using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    /// <summary>
    /// Class containing extension methods for working with the nfo configuration.
    /// </summary>
    public static class NfoConfigurationExtensions
    {
        /// <summary>
        /// Gets the nfo configuration.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <returns>The nfo configuration.</returns>
        public static XbmcMetadataOptions GetNfoConfiguration(this IConfigurationManager configurationManager)
         => configurationManager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");
    }
}
