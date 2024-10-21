global using System.Collections.Generic;

namespace MediaBrowser.XbmcMetadata.Configuration;

/// <summary>
/// Factory class for XBMC metadata configuration.
/// </summary>
public class NfoConfigurationFactory : IConfigurationFactory
{
    /// <inheritdoc />
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        return new[]
        {
            new ConfigurationStore
            {
                ConfigurationType = typeof(XbmcMetadataOptions),
                Key = "xbmcmetadata"
            }
        };
    }
}
