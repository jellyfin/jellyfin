#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Common.Configuration
{
    public interface IConfigurationManager
    {
        /// <summary>
        /// Occurs when [configuration updating].
        /// </summary>
        event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdating;

        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        event EventHandler<EventArgs> ConfigurationUpdated;

        /// <summary>
        /// Occurs when [named configuration updated].
        /// </summary>
        event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdated;

        /// <summary>
        /// Gets the application paths.
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

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.Object.</returns>
        object GetConfiguration(string key);

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Type.</returns>
        Type GetConfigurationType(string key);

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="configuration">The configuration.</param>
        void SaveConfiguration(string key, object configuration);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="factories">The factories.</param>
        void AddParts(IEnumerable<IConfigurationFactory> factories);
    }

    public static class ConfigurationManagerExtensions
    {
        public static T GetConfiguration<T>(this IConfigurationManager manager, string key)
        {
            return (T)manager.GetConfiguration(key);
        }
    }
}
