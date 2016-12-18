using MediaBrowser.Model.Plugins;
using System;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Interface IPlugin
    /// </summary>
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
        /// Gets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        bool IsFirstRun { get; }

        /// <summary>
        /// Gets the last date modified of the configuration
        /// </summary>
        /// <value>The configuration date last modified.</value>
        DateTime ConfigurationDateLastModified { get; }

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

        void SetStartupInfo(bool isFirstRun, Func<string, DateTime> dateModifiedFn, Action<string> directoryCreateFn);
    }
}