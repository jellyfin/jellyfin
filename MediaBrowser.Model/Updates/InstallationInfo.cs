using ProtoBuf;
using System;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class InstallationInfo
    /// </summary>
    [ProtoContract]
    public class InstallationInfo
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(1)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [ProtoMember(3)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the update class.
        /// </summary>
        /// <value>The update class.</value>
        [ProtoMember(4)]
        public PackageVersionClass UpdateClass { get; set; }

        /// <summary>
        /// Gets or sets the percent complete.
        /// </summary>
        /// <value>The percent complete.</value>
        [ProtoMember(5)]
        public double? PercentComplete { get; set; }
    }
}
