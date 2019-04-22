using System.Collections.Generic;
using Jellyfin.Common.Configuration;
using Jellyfin.Model.Configuration;

namespace Jellyfin.Controller.Library
{
    public class MetadataConfigurationStore : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
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
