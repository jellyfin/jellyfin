using System;

namespace Emby.Server.Implementations.Plugins
{
    /// <summary>
    /// Defines a Plugin manifest file.
    /// </summary>
    public class PluginManifest
    {
        /// <summary>
        /// Gets or sets the category of the plugin.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the changelog information.
        /// </summary>
        public string Changelog { get; set; }

        /// <summary>
        /// Gets or sets the description of the plugin.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the Global Unique Identifier for the plugin.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the Name of the plugin.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets an overview of the plugin.
        /// </summary>
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the owner of the plugin.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Gets or sets the compatibility version for the plugin.
        /// </summary>
        public string TargetAbi { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the plugin.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the Version number of the plugin.
        /// </summary>
        public string Version { get; set; }
    }
}
