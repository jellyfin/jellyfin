using MediaBrowser.Model.Extensions;
using ProtoBuf;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageVersionInfo
    /// </summary>
    [ProtoContract]
    public class PackageVersionInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the version STR.
        /// </summary>
        /// <value>The version STR.</value>
        [ProtoMember(2)]
        public string versionStr { get; set; }

        /// <summary>
        /// The _version
        /// </summary>
        private Version _version;
        /// <summary>
        /// Gets or sets the version.
        /// Had to make this an interpreted property since Protobuf can't handle Version
        /// </summary>
        /// <value>The version.</value>
        [IgnoreDataMember]
        public Version version
        {
            get { return _version ?? (_version = new Version(versionStr.ValueOrDefault("0.0.0.1"))); }
        }

        /// <summary>
        /// Gets or sets the classification.
        /// </summary>
        /// <value>The classification.</value>
        [ProtoMember(4)]
        public PackageVersionClass classification { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [ProtoMember(5)]
        public string description { get; set; }

        /// <summary>
        /// Gets or sets the required version STR.
        /// </summary>
        /// <value>The required version STR.</value>
        [ProtoMember(6)]
        public string requiredVersionStr { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        [ProtoMember(8)]
        public string sourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        [ProtoMember(9)]
        public Guid checksum { get; set; }

        /// <summary>
        /// Gets or sets the target filename.
        /// </summary>
        /// <value>The target filename.</value>
        [ProtoMember(10)]
        public string targetFilename { get; set; }
    }
}
