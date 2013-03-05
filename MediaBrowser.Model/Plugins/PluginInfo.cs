using MediaBrowser.Model.Updates;
using ProtoBuf;
using System;

namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    [ProtoContract]
    public class PluginInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [download to UI].
        /// </summary>
        /// <value><c>true</c> if [download to UI]; otherwise, <c>false</c>.</value>
        [ProtoMember(2)]
        public bool DownloadToUI { get; set; }

        /// <summary>
        /// Gets or sets the configuration date last modified.
        /// </summary>
        /// <value>The configuration date last modified.</value>
        [ProtoMember(3)]
        public DateTime ConfigurationDateLastModified { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [ProtoMember(4)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the name of the assembly file.
        /// </summary>
        /// <value>The name of the assembly file.</value>
        [ProtoMember(5)]
        public string AssemblyFileName { get; set; }

        /// <summary>
        /// Gets or sets the name of the configuration file.
        /// </summary>
        /// <value>The name of the configuration file.</value>
        [ProtoMember(6)]
        public string ConfigurationFileName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [ProtoMember(7)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        /// <value>The unique id.</value>
        [ProtoMember(9)]
        public Guid Id { get; set; }

        /// <summary>
        /// Whether or not this plug-in should be automatically updated when a
        /// compatible new version is released
        /// </summary>
        /// <value><c>true</c> if [enable auto update]; otherwise, <c>false</c>.</value>
        [ProtoMember(10)]
        public bool EnableAutoUpdate { get; set; }

        /// <summary>
        /// The classification of updates to which to subscribe.
        /// Options are: Dev, Beta or Release
        /// </summary>
        /// <value>The update class.</value>
        [ProtoMember(11)]
        public PackageVersionClass UpdateClass { get; set; }

        /// <summary>
        /// Gets or sets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        [ProtoMember(12)]
        public string MinimumRequiredUIVersion { get; set; }
    }
}
