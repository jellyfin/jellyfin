using MediaBrowser.Model.Configuration;
using System;

namespace MediaBrowser.Common.Configuration
{
    public interface IConfigurationManager
    {
        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        event EventHandler<EventArgs> ConfigurationUpdated;
        
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        IApplicationPaths CommonApplicationPaths { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        BaseApplicationConfiguration CommonConfiguration { get; }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        void SaveConfiguration();

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration);
    }
}
