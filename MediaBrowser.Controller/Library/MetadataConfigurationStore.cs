using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    public class MetadataConfigurationStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                     Key = "metadata",
                     ConfigurationType = typeof(MetadataConfiguration)
                }
            };
        }
    }

    public static class MetadataConfigurationExtensions
    {
        public static MetadataConfiguration GetMetadataConfiguration(this IConfigurationManager config)
        {
            return config.GetConfiguration<MetadataConfiguration>("metadata");
        }
    }
}
