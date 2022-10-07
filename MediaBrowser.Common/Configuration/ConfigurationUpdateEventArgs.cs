using System;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// <see cref="EventArgs" /> for the ConfigurationUpdated event.
    /// </summary>
    public class ConfigurationUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationUpdateEventArgs"/> class.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="newConfiguration">The new configuration.</param>
        public ConfigurationUpdateEventArgs(string key, object newConfiguration)
        {
            Key = key;
            NewConfiguration = newConfiguration;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets the new configuration.
        /// </summary>
        /// <value>The new configuration.</value>
        public object NewConfiguration { get; }
    }
}
