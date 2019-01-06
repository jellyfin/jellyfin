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
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationPaths" /> class.
        /// </summary>
        public ServerApplicationPaths(
            string programDataPath,
            string appFolderPath,
            string applicationResourcesPath,
            string logDirectoryPath = null,
            string configurationDirectoryPath = null)
            : base(programDataPath, appFolderPath, logDirectoryPath, configurationDirectoryPath)
        {
            ApplicationResourcesPath = applicationResourcesPath;
        }

        public string ApplicationResourcesPath { get; private set; }

        /// <summary>
        /// Gets the path to the base root media directory
        /// </summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "root");
            }
        }

        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        public string DefaultUserViewsPath
        {
            get
            {
                return Path.Combine(RootFolderPath, "default");
            }
        }

        /// <summary>
        /// Gets the path to localization data.
        /// </summary>
        /// <value>The localization path.</value>
        public string LocalizationPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "localization");
            }
        }

        /// <summary>
        /// Gets the path to the People directory
        /// </summary>
        /// <value>The people path.</value>
        public string PeoplePath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "People");
            }
        }

        public string ArtistsPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "artists");
            }
        }

        /// <summary>
        /// Gets the path to the Genre directory
        /// </summary>
        /// <value>The genre path.</value>
        public string GenrePath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "Genre");
            }
        }

        /// <summary>
        /// Gets the path to the Genre directory
        /// </summary>
        /// <value>The genre path.</value>
        public string MusicGenrePath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "MusicGenre");
            }
        }

        /// <summary>
        /// Gets the path to the Studio directory
        /// </summary>
        /// <value>The studio path.</value>
        public string StudioPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "Studio");
            }
        }

        /// <summary>
        /// Gets the path to the Year directory
        /// </summary>
        /// <value>The year path.</value>
        public string YearPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "Year");
            }
        }

        /// <summary>
        /// Gets the path to the General IBN directory
        /// </summary>
        /// <value>The general path.</value>
        public string GeneralPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "general");
            }
        }

        /// <summary>
        /// Gets the path to the Ratings IBN directory
        /// </summary>
        /// <value>The ratings path.</value>
        public string RatingsPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "ratings");
            }
        }

        /// <summary>
        /// Gets the media info images path.
        /// </summary>
        /// <value>The media info images path.</value>
        public string MediaInfoImagesPath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "mediainfo");
            }
        }

        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        /// <value>The user configuration directory path.</value>
        public string UserConfigurationDirectoryPath
        {
            get
            {
                return Path.Combine(ConfigurationDirectoryPath, "users");
            }
        }

        private string _defaultTranscodingTempPath;
        public string DefaultTranscodingTempPath
        {
            get
            {
                return _defaultTranscodingTempPath ?? (_defaultTranscodingTempPath = Path.Combine(ProgramDataPath, "transcoding-temp"));
            }
        }

        private string _transcodingTempPath;
        public string TranscodingTempPath
        {
            get
            {
                return _transcodingTempPath ?? (_transcodingTempPath = DefaultTranscodingTempPath);
            }
            set
            {
                _transcodingTempPath = value;
            }
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

        /// <summary>
        /// Gets the game genre path.
        /// </summary>
        /// <value>The game genre path.</value>
        public string GameGenrePath
        {
            get
            {
                return Path.Combine(InternalMetadataPath, "GameGenre");
            }
        }

        private string _internalMetadataPath;
        public string InternalMetadataPath
        {
            get
            {
                return _internalMetadataPath ?? (_internalMetadataPath = Path.Combine(DataPath, "metadata"));
            }
            set
            {
                _internalMetadataPath = value;
            }
        }

        private const string _virtualInternalMetadataPath = "%MetadataPath%";
        public string VirtualInternalMetadataPath
        {
            get
            {
                return _virtualInternalMetadataPath;
            }
        }

    }
}
