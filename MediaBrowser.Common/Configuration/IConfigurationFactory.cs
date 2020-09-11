using System.Collections.Generic;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Provides an interface to retrieve a configuration store. Classes with this interface are scanned for at
    /// application start to dynamically register configuration for various modules/plugins.
    /// </summary>
    public interface IConfigurationFactory
    {
        /// <summary>
        /// Get the configuration store for this module.
        /// </summary>
        /// <returns>The configuration store.</returns>
        IEnumerable<ConfigurationStore> GetConfigurations();
    }
}
