using MediaBrowser.Model.Updates;
using System;

namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the configuration date last modified.
        /// </summary>
        /// <value>The configuration date last modified.</value>
        public DateTime ConfigurationDateLastModified { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the assembly file.
        /// </summary>
        /// <value>The name of the assembly file.</value>
        public string AssemblyFileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration file.
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public string ConfigurationFileName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Whether or not this plug-in should be automatically updated when a
        /// compatible new version is released
        /// </summary>
        /// <value><c>true</c> if [enable auto update]; otherwise, <c>false</c>.</value>
        public bool EnableAutoUpdate { get; set; }

        /// <summary>
        /// The classification of updates to which to subscribe.
        /// Options are: Dev, Beta or Release
        /// </summary>
        /// <value>The update class.</value>
        public PackageVersionClass UpdateClass { get; set; }

        /// <summary>
        /// Gets or sets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        public string MinimumRequiredUIVersion { get; set; }
    }
}
