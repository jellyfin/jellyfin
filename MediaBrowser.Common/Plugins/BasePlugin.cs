using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Provides a common base class for all plugins.
    /// </summary>
    public abstract class BasePlugin : IPlugin, IPluginAssembly
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public virtual string Description => string.Empty;

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public virtual Guid Id { get; private set; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get; private set; }

        /// <summary>
        /// Gets the path to the assembly file.
        /// </summary>
        /// <value>The assembly file path.</value>
        public string AssemblyFilePath { get; private set; }

        /// <summary>
        /// Gets the full path to the data folder, where the plugin can store any miscellaneous files needed.
        /// </summary>
        /// <value>The data folder path.</value>
        public string DataFolderPath { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the plugin can be uninstalled.
        /// </summary>
        public bool CanUninstall => !Path.GetDirectoryName(AssemblyFilePath)
            .Equals(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), StringComparison.InvariantCulture);

        /// <summary>
        /// Gets the plugin info.
        /// </summary>
        /// <returns>PluginInfo.</returns>
        public virtual PluginInfo GetPluginInfo()
        {
            var info = new PluginInfo(
                Name,
                Version,
                Description,
                Id,
                CanUninstall);

            return info;
        }

        /// <summary>
        /// Called just before the plugin is uninstalled from the server.
        /// </summary>
        public virtual void OnUninstalling()
        {
        }

        /// <inheritdoc />
        public void SetAttributes(string assemblyFilePath, string dataFolderPath, Version assemblyVersion)
        {
            AssemblyFilePath = assemblyFilePath;
            DataFolderPath = dataFolderPath;
            Version = assemblyVersion;
        }

        /// <inheritdoc />
        public void SetId(Guid assemblyId)
        {
            Id = assemblyId;
        }
    }
}
