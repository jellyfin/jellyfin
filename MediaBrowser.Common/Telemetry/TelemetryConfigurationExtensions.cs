using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Common.Telemetry;

/// <summary>
/// Defines the <see cref="TelemetryConfigurationExtensions" />.
/// </summary>
public static class TelemetryConfigurationExtensions
{
    /// <summary>
    /// Retrieves the telemetry configuration.
    /// </summary>
    /// <param name="config">The <see cref="IConfigurationManager"/>.</param>
    /// <returns>The <see cref="OpenTelemetryOptions"/>.</returns>
    public static OpenTelemetryOptions GetTelemetryConfiguration(this IConfigurationManager config)
    {
        return config.GetConfiguration<OpenTelemetryOptions>(TelemetryConfigurationStore.StoreKey);
    }
}
