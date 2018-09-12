using MediaBrowser.Common.Configuration;
using Emby.Dlna.Configuration;
using System.Collections.Generic;

namespace Emby.Dlna
{
    public static class ConfigurationExtension
    {
        public static DlnaOptions GetDlnaConfiguration(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<DlnaOptions>("dlna");
        }
    }

    public class DlnaConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                    Key = "dlna",
                    ConfigurationType = typeof (DlnaOptions)
                }
            };
        }
    }
}
