#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Library;

public class MetadataConfigurationStore : IConfigurationFactory
{
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        return
        [
            new ConfigurationStore
            {
                Key = "metadata",
                ConfigurationType = typeof(MetadataConfiguration)
            }
        ];
    }
}
