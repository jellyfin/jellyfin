#nullable enable
using System;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Defines a Plugin manifest file.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Gets or sets the category of the plugin.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the changelog information.
        /// </summary>
        public string Changelog { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the plugin.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Global Unique Identifier for the plugin.
        /// </summary>
#pragma warning disable CA1720 // Identifier contains type name
        public Guid Guid { get; set; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Gets or sets the Name of the plugin.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets an overview of the plugin.
        /// </summary>
        public string Overview { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner of the plugin.
        /// </summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the compatibility version for the plugin.
        /// </summary>
        public string TargetAbi { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the upper compatibility version for the plugin.
        /// </summary>
        public string MaxAbi { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the plugin.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the Version number of the plugin.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating the operational status of this plugin.
        /// </summary>
        public PluginStatus Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this plugin should automatically update.
        /// </summary>
        public bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this plugin has an image.
        /// Image must be located in the local plugin folder.
        /// </summary>
        public string? ImageUrl { get; set; }
    }
}
