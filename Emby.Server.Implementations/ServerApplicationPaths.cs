using System;
using System.IO;
using Emby.Server.Implementations.AppBase;
using MediaBrowser.Controller;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Extends BaseApplicationPaths to add paths that are only applicable on the server
    /// </summary>
    public class ServerApplicationPaths : BaseApplicationPaths, IServerApplicationPaths
    {
        private string _defaultTranscodingTempPath;
        private string _transcodingTempPath;
        private string _internalMetadataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerApplicationPaths" /> class.
        /// </summary>
        public ServerApplicationPaths(
            string programDataPath,
            string logDirectoryPath,
            string configurationDirectoryPath,
            string cacheDirectoryPath,
            string webDirectoryPath)
            : base(programDataPath,
                logDirectoryPath,
                configurationDirectoryPath,
                cacheDirectoryPath,
                webDirectoryPath)
        {
        }

        public string ApplicationResourcesPath { get; } = AppContext.BaseDirectory;

        /// <summary>
        /// Gets the path to the base root media directory.
        /// </summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath => Path.Combine(ProgramDataPath, "root");

        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        public string DefaultUserViewsPath => Path.Combine(RootFolderPath, "default");

        /// <summary>
        /// Gets the path to localization data.
        /// </summary>
        /// <value>The localization path.</value>
        public string LocalizationPath => Path.Combine(ProgramDataPath, "localization");

        /// <summary>
        /// Gets the path to the People directory.
        /// </summary>
        /// <value>The people path.</value>
        public string PeoplePath => Path.Combine(InternalMetadataPath, "People");

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
        /// Gets the path to the General IBN directory.
        /// </summary>
        /// <value>The general path.</value>
        public string GeneralPath => Path.Combine(InternalMetadataPath, "general");

        /// <summary>
        /// Gets the path to the Ratings IBN directory.
        /// </summary>
        /// <value>The ratings path.</value>
        public string RatingsPath => Path.Combine(InternalMetadataPath, "ratings");

        /// <summary>
        /// Gets the media info images path.
        /// </summary>
        /// <value>The media info images path.</value>
        public string MediaInfoImagesPath => Path.Combine(InternalMetadataPath, "mediainfo");

        /// <summary>
        /// Gets the path to the user configuration directory.
        /// </summary>
        /// <value>The user configuration directory path.</value>
        public string UserConfigurationDirectoryPath => Path.Combine(ConfigurationDirectoryPath, "users");

        public string DefaultTranscodingTempPath => _defaultTranscodingTempPath ?? (_defaultTranscodingTempPath = Path.Combine(ProgramDataPath, "transcoding-temp"));

        public string TranscodingTempPath
        {
            get => _transcodingTempPath ?? (_transcodingTempPath = DefaultTranscodingTempPath);
            set => _transcodingTempPath = value;
        }

        public string GetTranscodingTempPath()
        {
            var path = TranscodingTempPath;

            if (!string.Equals(path, DefaultTranscodingTempPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.CreateDirectory(path);

                    var testPath = Path.Combine(path, Guid.NewGuid().ToString());
                    Directory.CreateDirectory(testPath);
                    Directory.Delete(testPath);

                    return path;
                }
                catch
                {
                }
            }

            path = DefaultTranscodingTempPath;
            Directory.CreateDirectory(path);
            return path;
        }

        public string InternalMetadataPath
        {
            get => _internalMetadataPath ?? (_internalMetadataPath = Path.Combine(DataPath, "metadata"));
            set => _internalMetadataPath = value;
        }

        public string VirtualInternalMetadataPath { get; } = "%MetadataPath%";
    }
}
