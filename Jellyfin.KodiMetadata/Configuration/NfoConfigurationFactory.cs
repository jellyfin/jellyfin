#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.KodiMetadata.Configuration
{
    public class NfoConfigurationFactory : IConfigurationFactory
    {
        /// <inheritdoc />
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(XbmcMetadataOptions),
                     Key = "xbmcmetadata"
                }
            };
        }
    }
}
