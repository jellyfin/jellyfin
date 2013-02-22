using System;
using System.IO;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface IConfigurationPage
    /// </summary>
    public interface IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        ConfigurationPageType ConfigurationPageType { get; }

        /// <summary>
        /// Gets the plugin id.
        /// </summary>
        /// <value>The plugin id.</value>
        Guid? PluginId { get; }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        Stream GetHtmlStream();

        /// <summary>
        /// Gets the version. Typically taken from Plugin.Version
        /// </summary>
        /// <value>The version.</value>
        string Version { get; }

        /// <summary>
        /// For http caching purposes. Typically taken from Plugin.AssemblyDateLastModified
        /// </summary>
        DateTime DateLastModified { get; }
    }

    /// <summary>
    /// Enum ConfigurationPageType
    /// </summary>
    public enum ConfigurationPageType
    {
        /// <summary>
        /// The plugin configuration
        /// </summary>
        PluginConfiguration,
        /// <summary>
        /// The none
        /// </summary>
        None
    }
}
