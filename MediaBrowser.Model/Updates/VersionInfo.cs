#nullable disable

using System;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageVersionInfo.
    /// </summary>
    public class VersionInfo
    {
        private Version _version;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string version
        {
            get
            {
                return _version == null ? string.Empty : _version.ToString();
            }

            set
            {
                _version = Version.Parse(value);
            }
        }

        /// <summary>
        /// Gets the version as a <see cref="Version"/>.
        /// </summary>
        public Version VersionNumber => _version;

        /// <summary>
        /// Gets or sets the changelog for this version.
        /// </summary>
        /// <value>The changelog.</value>
        public string changelog { get; set; }

        /// <summary>
        /// Gets or sets the ABI that this version was built against.
        /// </summary>
        /// <value>The target ABI version.</value>
        public string targetAbi { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string sourceUrl { get; set; }

        /// <summary>
        /// Gets or sets a checksum for the binary.
        /// </summary>
        /// <value>The checksum.</value>
        public string checksum { get; set; }

        /// <summary>
        /// Gets or sets a timestamp of when the binary was built.
        /// </summary>
        /// <value>The timestamp.</value>
        public string timestamp { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string repositoryName { get; set; }

        /// <summary>
        /// Gets or sets the repository url.
        /// </summary>
        public string repositoryUrl { get; set; }
    }
}
