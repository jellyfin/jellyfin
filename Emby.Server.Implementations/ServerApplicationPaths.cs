using System.IO;
using Emby.Server.Implementations.AppBase;
using MediaBrowser.Controller;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Extends BaseApplicationPaths to add paths that are only applicable on the server.
    /// </summary>
    public class ServerApplicationPaths : BaseApplicationPaths, IServerApplicationPaths
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerApplicationPaths" /> class.
        /// </summary>
        /// <param name="programDataPath">The path for Jellyfin's data.</param>
        /// <param name="logDirectoryPath">The path for Jellyfin's logging directory.</param>
        /// <param name="configurationDirectoryPath">The path for Jellyfin's configuration directory.</param>
        /// <param name="cacheDirectoryPath">The path for Jellyfin's cache directory.</param>
        /// <param name="webDirectoryPath">The path for Jellyfin's web UI.</param>
        public ServerApplicationPaths(
            string programDataPath,
            string logDirectoryPath,
            string configurationDirectoryPath,
            string cacheDirectoryPath,
            string webDirectoryPath)
            : base(
                programDataPath,
                logDirectoryPath,
                configurationDirectoryPath,
                cacheDirectoryPath,
                webDirectoryPath)
        {
            // ProgramDataPath cannot change when the server is running, so cache these to avoid allocations.
            RootFolderPath = Path.Join(ProgramDataPath, "root");
            DefaultUserViewsPath = Path.Combine(RootFolderPath, "default");
            DefaultInternalMetadataPath = Path.Combine(ProgramDataPath, "metadata");
            InternalMetadataPath = DefaultInternalMetadataPath;
        }

        /// <summary>
        /// Gets the path to the base root media directory.
        /// </summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath { get; }

        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        public string DefaultUserViewsPath { get; }

        /// <summary>
        /// Gets the path to the People directory.
        /// </summary>
        /// <value>The people path.</value>
        public string PeoplePath => Path.Combine(InternalMetadataPath, "People");

        /// <inheritdoc />
        public string ArtistsPath => Path.Combine(InternalMetadataPath, "artists");

        /// <summary>
        /// Gets the path to the Genre directory.
        /// </summary>
        /// <value>The genre path.</value>
        public string GenrePath => Path.Combine(InternalMetadataPath, "Genre");

        /// <summary>
        /// Gets the path to the Genre directory.
        /// </summary>
        /// <value>The genre path.</value>
        public string MusicGenrePath => Path.Combine(InternalMetadataPath, "MusicGenre");

        /// <summary>
        /// Gets the path to the Studio directory.
        /// </summary>
        /// <value>The studio path.</value>
        public string StudioPath => Path.Combine(InternalMetadataPath, "Studio");

        /// <summary>
        /// Gets the path to the Year directory.
        /// </summary>
        /// <value>The year path.</value>
        public string YearPath => Path.Combine(InternalMetadataPath, "Year");

        /// <summary>
        /// Gets the path to the user configuration directory.
        /// </summary>
        /// <value>The user configuration directory path.</value>
        public string UserConfigurationDirectoryPath => Path.Combine(ConfigurationDirectoryPath, "users");

        /// <inheritdoc/>
        public string DefaultInternalMetadataPath { get; }

        /// <inheritdoc />
        public string InternalMetadataPath { get; set; }

        /// <inheritdoc />
        public string VirtualInternalMetadataPath => "%MetadataPath%";
    }
}
