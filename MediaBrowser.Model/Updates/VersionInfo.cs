using System.Text.Json.Serialization;
using SysVersion = System.Version;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Defines the <see cref="VersionInfo"/> class.
    /// </summary>
    public class VersionInfo
    {
        private SysVersion? _version;

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [JsonPropertyName("version")]
        public string Version
        {
            get => _version is null ? string.Empty : _version.ToString();

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
        [JsonPropertyName("changelog")]
        public string? Changelog { get; set; }

        /// <summary>
        /// Gets or sets the ABI that this version was built against.
        /// </summary>
        /// <value>The target ABI version.</value>
        [JsonPropertyName("targetAbi")]
        public string? TargetAbi { get; set; }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        /// <value>The source URL.</value>
        [JsonPropertyName("sourceUrl")]
        public string? SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets a checksum for the binary.
        /// </summary>
        /// <value>The checksum.</value>
        [JsonPropertyName("checksum")]
        public string? Checksum { get; set; }

        /// <summary>
        /// Gets or sets a timestamp of when the binary was built.
        /// </summary>
        /// <value>The timestamp.</value>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        [JsonPropertyName("repositoryName")]
        public string RepositoryName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the repository url.
        /// </summary>
        [JsonPropertyName("repositoryUrl")]
        public string RepositoryUrl { get; set; } = string.Empty;
    }
}
