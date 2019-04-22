using System.Collections.Generic;
using Jellyfin.Dlna.Configuration;
using Jellyfin.Common.Configuration;

namespace Jellyfin.Dlna
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
