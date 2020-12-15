#nullable enable

using System;

namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginInfo"/> class.
        /// </summary>
        /// <param name="name">The plugin name.</param>
        /// <param name="version">The plugin <see cref="Version"/>.</param>
        /// <param name="description">The plugin description.</param>
        /// <param name="id">The <see cref="Guid"/>.</param>
        /// <param name="canUninstall">True if this plugin can be uninstalled.</param>
        public PluginInfo(string name, Version version, string description, Guid id, bool canUninstall)
        {
            Name = name;
            Version = version;
            Description = description;
            Id = id;
            CanUninstall = canUninstall;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration file.
        /// </summary>
        public string? ConfigurationFileName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plugin can be uninstalled.
        /// </summary>
        public bool CanUninstall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this plugin has a valid image.
        /// </summary>
        public bool HasImage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the status of the plugin.
        /// </summary>
        public PluginStatus Status { get; set; }
    }
}
