#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Interface IPlugin.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        string Description { get; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        /// <value>The version.</value>
        Version Version { get; }

        /// <summary>
        /// Gets the path to the assembly file.
        /// </summary>
        /// <value>The assembly file path.</value>
        string AssemblyFilePath { get; }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed.
        /// </summary>
        /// <value>The data folder path.</value>
        string DataFolderPath { get; }

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

    public interface IHasPluginConfiguration
    {
        /// <summary>
        /// Gets the type of configuration this plugin uses.
        /// </summary>
        /// <value>The type of the configuration.</value>
        Type ConfigurationType { get; }

        /// <summary>
        /// Gets the plugin's configuration.
        /// </summary>
        /// <value>The configuration.</value>
        BasePluginConfiguration Configuration { get; }

        /// <summary>
        /// Completely overwrites the current configuration with a new copy
        /// Returns true or false indicating success or failure.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="ArgumentNullException"><c>configuration</c> is <c>null</c>.</exception>
        void UpdateConfiguration(BasePluginConfiguration configuration);

        void SetStartupInfo(Action<string> directoryCreateFn);
    }
}
