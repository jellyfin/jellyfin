#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;
using Emby.Dlna.Configuration;
using MediaBrowser.Common.Configuration;

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
