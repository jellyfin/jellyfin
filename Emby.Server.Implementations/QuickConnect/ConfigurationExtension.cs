using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.QuickConnect
{
    /// <summary>
    /// Configuration extension to support persistent quick connect configuration.
    /// </summary>
    public static class ConfigurationExtension
    {
        /// <summary>
        /// Return the current quick connect configuration.
        /// </summary>
        /// <param name="manager">Configuration manager.</param>
        /// <returns>Current quick connect configuration.</returns>
        public static QuickConnectConfiguration GetQuickConnectConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<QuickConnectConfiguration>("quickconnect");
        }
    }

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
