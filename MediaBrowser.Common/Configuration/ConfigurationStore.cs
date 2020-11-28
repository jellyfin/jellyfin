using System;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Describes a single entry in the application configuration.
    /// </summary>
    public class ConfigurationStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationStore"/> class.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="configurationType">Configuration type.</param>
        public ConfigurationStore(string key, Type configurationType)
        {
            Key = key;
            ConfigurationType = configurationType;
        }

        /// <summary>
        /// Gets or sets the unique identifier for the configuration.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the type used to store the data for this configuration entry.
        /// </summary>
        public Type ConfigurationType { get; set; }
    }
}
