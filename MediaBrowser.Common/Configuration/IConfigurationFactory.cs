using System;
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

    /// <summary>
    /// Describes a single entry in the application configuration.
    /// </summary>
    public class ConfigurationStore
    {
        /// <summary>
        /// Gets or sets the unique identifier for the configuration.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the type used to store the data for this configuration entry.
        /// </summary>
        public Type ConfigurationType { get; set; }
    }

    /// <summary>
    /// A configuration store that can be validated.
    /// </summary>
    public interface IValidatingConfiguration
    {
        /// <summary>
        /// Validation method to be invoked before saving the configuration.
        /// </summary>
        /// <param name="oldConfig">The old configuration.</param>
        /// <param name="newConfig">The new configuration.</param>
        void Validate(object oldConfig, object newConfig);
    }
}
