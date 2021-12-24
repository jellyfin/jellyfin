using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Networking.Configuration
{
    /// <summary>
    /// Defines the <see cref="NetworkConfigurationFactory" />.
    /// </summary>
    public class NetworkConfigurationFactory : IConfigurationFactory
    {
        /// <summary>
        /// The GetConfigurations.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{ConfigurationStore}"/>.</returns>
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new NetworkConfigurationStore()
            };
        }
    }
}
