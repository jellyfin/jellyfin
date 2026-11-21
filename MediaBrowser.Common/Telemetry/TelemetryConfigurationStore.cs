using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Common.Telemetry;

/// <summary>
/// A configuration that stores telemetry related settings.
/// </summary>
public class TelemetryConfigurationStore : ConfigurationStore
{
    /// <summary>
    /// The name of the configuration in the storage.
    /// </summary>
    public const string StoreKey = "telemetry";

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryConfigurationStore"/> class.
    /// </summary>
    public TelemetryConfigurationStore()
    {
        ConfigurationType = typeof(OpenTelemetryOptions);
        Key = StoreKey;
    }
}
