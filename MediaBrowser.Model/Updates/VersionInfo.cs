#nullable enable

using SysVersion = System.Version;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageVersionInfo.
    /// </summary>
    public class VersionInfo
    {
        private SysVersion? _version;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version
        {
            get => _version == null ? string.Empty : _version.ToString();

            set => _version = SysVersion.Parse(value);
        }

        /// <summary>
        /// Gets the version as a <see cref="SysVersion"/>.
        /// </summary>
        public SysVersion VersionNumber => _version ?? new SysVersion(0, 0, 0);

        /// <summary>
        /// Gets or sets the changelog for this version.
        /// </summary>
        /// <value>The changelog.</value>
        public string? Changelog { get; set; }

        /// <summary>
        /// Gets or sets the ABI that this version was built against.
        /// </summary>
        /// <value>The target ABI version.</value>
        public string? TargetAbi { get; set; }

        /// <summary>
        /// Gets or sets the maximum ABI that this version will work with.
        /// </summary>
        /// <value>The target ABI version.</value>
        public string? MaxAbi { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets a checksum for the binary.
        /// </summary>
        /// <value>The checksum.</value>
        public string? Checksum { get; set; }

        /// <summary>
        /// Gets or sets a timestamp of when the binary was built.
        /// </summary>
        /// <value>The timestamp.</value>
        public string? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository url.
        /// </summary>
        public string RepositoryUrl { get; set; } = string.Empty;
    }
}
