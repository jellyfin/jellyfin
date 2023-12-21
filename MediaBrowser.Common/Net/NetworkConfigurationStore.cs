using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Common.Net;

/// <summary>
/// A configuration that stores network related settings.
/// </summary>
public class NetworkConfigurationStore : ConfigurationStore
{
    /// <summary>
    /// The name of the configuration in the storage.
    /// </summary>
    public const string StoreKey = "network";

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConfigurationStore"/> class.
    /// </summary>
    public NetworkConfigurationStore()
    {
        ConfigurationType = typeof(NetworkConfiguration);
        Key = StoreKey;
    }
}
