using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Configuration factory for quick connect.
    /// </summary>
    public class QuickConnectConfigurationFactory : IConfigurationFactory
    {
        /// <summary>
        /// Returns the current quick connect configuration.
        /// </summary>
        /// <returns>Current quick connect configuration.</returns>
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                    Key = "quickconnect",
                    ConfigurationType = typeof(QuickConnectConfiguration)
                }
            };
        }
    }
}
