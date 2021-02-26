#nullable disable

using System;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class InstallationInfo.
    /// </summary>
    public class InstallationInfo
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <value>The Id.</value>
        [JsonPropertyName("Guid")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets the changelog for this version.
        /// </summary>
        /// <value>The changelog.</value>
        public string Changelog { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets a checksum for the binary.
        /// </summary>
        /// <value>The checksum.</value>
        public string Checksum { get; set; }

        /// <summary>
        /// Gets or sets package information for the installation.
        /// </summary>
        /// <value>The package information.</value>
        public PackageInfo PackageInfo { get; set; }
    }
}
