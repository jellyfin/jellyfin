using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Common.Telemetry;

/// <summary>
/// Defines the <see cref="TelemetryConfigurationFactory" />.
/// </summary>
public class TelemetryConfigurationFactory : IConfigurationFactory
{
    /// <summary>
    /// The GetConfigurations.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{ConfigurationStore}"/>.</returns>
    public IEnumerable<ConfigurationStore> GetConfigurations()
    {
        return new[]
        {
            new TelemetryConfigurationStore()
        };
    }
}
