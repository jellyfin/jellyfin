#nullable disable

using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Defines the <see cref="IPlugin" />.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the Description.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets the path to the assembly file.
        /// </summary>
        string AssemblyFilePath { get; }

        /// <summary>
        /// Gets a value indicating whether the plugin can be uninstalled.
        /// </summary>
        bool CanUninstall { get; }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed.
        /// </summary>
        string DataFolderPath { get; }

        /// <summary>
        /// Gets the <see cref="PluginInfo"/>.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        PluginInfo GetPluginInfo();

        /// <summary>
        /// Called when just before the plugin is uninstalled from the server.
        /// </summary>
        void OnUninstalling();
    }
}
