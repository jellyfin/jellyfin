using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;

namespace MediaBrowser.Common.Plugins
{
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a core plugin.
        /// </summary>
        /// <value><c>true</c> if this instance is a core plugin; otherwise, <c>false</c>.</value>
        bool IsCorePlugin { get; }

        /// <summary>
        /// Gets the type of configuration this plugin uses
        /// </summary>
        /// <value>The type of the configuration.</value>
        Type ConfigurationType { get; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the plugin version
        /// </summary>
        /// <value>The version.</value>
        Version Version { get; }

        /// <summary>
        /// Gets the name the assembly file
        /// </summary>
        /// <value>The name of the assembly file.</value>
        string AssemblyFileName { get; }

        /// <summary>
        /// Gets the last date modified of the configuration
        /// </summary>
        /// <value>The configuration date last modified.</value>
        DateTime ConfigurationDateLastModified { get; }

        /// <summary>
        /// Gets the last date modified of the plugin
        /// </summary>
        /// <value>The assembly date last modified.</value>
        DateTime AssemblyDateLastModified { get; }

        /// <summary>
        /// Gets the path to the assembly file
        /// </summary>
        /// <value>The assembly file path.</value>
        string AssemblyFilePath { get; }

        /// <summary>
        /// Gets the plugin's configuration
        /// </summary>
        /// <value>The configuration.</value>
        BasePluginConfiguration Configuration { get; }

        /// <summary>
        /// Gets the name of the configuration file. Subclasses should override
        /// </summary>
        /// <value>The name of the configuration file.</value>
        string ConfigurationFileName { get; }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        /// <value>The configuration file path.</value>
        string ConfigurationFilePath { get; }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed
        /// </summary>
        /// <value>The data folder path.</value>
        string DataFolderPath { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        ILogger Logger { get; }

        /// <summary>
        /// Starts the plugin.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">kernel</exception>
        void Initialize(IKernel kernel, IXmlSerializer xmlSerializer, ILogger logger);

        /// <summary>
        /// Disposes the plugins. Undos all actions performed during Init.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Saves the current configuration to the file system
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot call Plugin.SaveConfiguration from the UI.</exception>
        void SaveConfiguration();

        /// <summary>
        /// Completely overwrites the current configuration with a new copy
        /// Returns true or false indicating success or failure
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        void UpdateConfiguration(BasePluginConfiguration configuration);

        /// <summary>
        /// Gets the plugin info.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        PluginInfo GetPluginInfo();

        /// <summary>
        /// Called when just before the plugin is uninstalled from the server.
        /// </summary>
        void OnUninstalling();
    }
}