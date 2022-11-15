using MediaBrowser.Common.Configuration;

namespace Jellyfin.Networking.Configuration
{
    /// <summary>
    /// Defines the <see cref="NetworkConfigurationExtensions" />.
    /// </summary>
    public static class NetworkConfigurationExtensions
    {
        /// <summary>
        /// Retrieves the network configuration.
        /// </summary>
        /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
        /// <returns>The <see cref="NetworkConfiguration"/>.</returns>
        public static NetworkConfiguration GetNetworkConfiguration(this IConfigurationManager config)
        {
            return config.GetConfiguration<NetworkConfiguration>("network");
        }
    }
}
