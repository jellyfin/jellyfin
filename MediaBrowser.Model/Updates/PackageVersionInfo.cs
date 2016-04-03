using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageVersionInfo
    /// </summary>
    public class PackageVersionInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the guid.
        /// </summary>
        /// <value>The guid.</value>
        public string guid { get; set; }

        /// <summary>
        /// Gets or sets the version STR.
        /// </summary>
        /// <value>The version STR.</value>
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
            get { return _version ?? (_version = new Version(ValueOrDefault(versionStr, "0.0.0.1"))); }
        }

        /// <summary>
        /// Values the or default.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="def">The def.</param>
        /// <returns>System.String.</returns>
        private static string ValueOrDefault(string str, string def)
        {
            return string.IsNullOrEmpty(str) ? def : str;
        }

        /// <summary>
        /// Gets or sets the classification.
        /// </summary>
        /// <value>The classification.</value>
        public PackageVersionClass classification { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string description { get; set; }

        /// <summary>
        /// Gets or sets the required version STR.
        /// </summary>
        /// <value>The required version STR.</value>
        public string requiredVersionStr { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string sourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string checksum { get; set; }

        /// <summary>
        /// Gets or sets the target filename.
        /// </summary>
        /// <value>The target filename.</value>
        public string targetFilename { get; set; }

        public string infoUrl { get; set; }
    }
}