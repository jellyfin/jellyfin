using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Jellyfin.LiveTv.Configuration;

/// <summary>
/// <see cref="IConfigurationFactory" /> implementation for <see cref="LiveTvOptions" />.
/// </summary>
public class LiveTvConfigurationFactory : IConfigurationFactory
{
    /// <inheritdoc />
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        return new[]
        {
            new ConfigurationStore
            {
                ConfigurationType = typeof(LiveTvOptions),
                Key = "livetv"
            }
        };
    }
}
