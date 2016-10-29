using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
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
            return new List<ConfigurationStore>
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
