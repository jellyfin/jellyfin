#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.QuickConnect
{
    public static class ConfigurationExtension
    {
        public static QuickConnectConfiguration GetQuickConnectConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<QuickConnectConfiguration>("quickconnect");
        }
    }

    public class QuickConnectConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
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
