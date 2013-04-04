using MediaBrowser.Common.Implementations;
using MediaBrowser.Controller;
using System.IO;

namespace MediaBrowser.Server.Implementations
{
    /// <summary>
    /// Extends BaseApplicationPaths to add paths that are only applicable on the server
    /// </summary>
    public class ServerApplicationPaths : BaseApplicationPaths, IServerApplicationPaths
    {
#if (DEBUG)
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerApplicationPaths" /> class.
        /// </summary>
        public ServerApplicationPaths()
            : base(true)
        {
        }
#else
        public ServerApplicationPaths()
            : base(false)
        {
        }
#endif
        /// <summary>
        /// The _root folder path
        /// </summary>
        private string _rootFolderPath;
        /// <summary>
        /// Gets the path to the base root media directory
        /// </summary>
        /// <value>The root folder path.</value>
        public string RootFolderPath
        {
            get
            {
                if (_rootFolderPath == null)
                {
                    _rootFolderPath = Path.Combine(ProgramDataPath, "Root");
                    if (!Directory.Exists(_rootFolderPath))
                    {
                        Directory.CreateDirectory(_rootFolderPath);
                    }
                }
                return _rootFolderPath;
            }
        }

        /// <summary>
        /// The _default user views path
        /// </summary>
        private string _defaultUserViewsPath;
        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        public string DefaultUserViewsPath
        {
            get
            {
                if (_defaultUserViewsPath == null)
                {
                    _defaultUserViewsPath = Path.Combine(RootFolderPath, "Default");
                    if (!Directory.Exists(_defaultUserViewsPath))
                    {
                        Directory.CreateDirectory(_defaultUserViewsPath);
                    }
                }
                return _defaultUserViewsPath;
            }
        }

        /// <summary>
        /// The _localization path
        /// </summary>
        private string _localizationPath;
        /// <summary>
        /// Gets the path to localization data.
        /// </summary>
        /// <value>The localization path.</value>
        public string LocalizationPath
        {
            get
            {
                if (_localizationPath == null)
                {
                    _localizationPath = Path.Combine(ProgramDataPath, "Localization");
                    if (!Directory.Exists(_localizationPath))
                    {
                        Directory.CreateDirectory(_localizationPath);
                    }
                }
                return _localizationPath;
            }
        }

        /// <summary>
        /// The _ibn path
        /// </summary>
        private string _ibnPath;
        /// <summary>
        /// Gets the path to the Images By Name directory
        /// </summary>
        /// <value>The images by name path.</value>
        public string ImagesByNamePath
        {
            get
            {
                if (_ibnPath == null)
                {
                    _ibnPath = Path.Combine(ProgramDataPath, "ImagesByName");
                    if (!Directory.Exists(_ibnPath))
                    {
                        Directory.CreateDirectory(_ibnPath);
                    }
                }

                return _ibnPath;
            }
        }

        /// <summary>
        /// The _people path
        /// </summary>
        private string _peoplePath;
        /// <summary>
        /// Gets the path to the People directory
        /// </summary>
        /// <value>The people path.</value>
        public string PeoplePath
        {
            get
            {
                if (_peoplePath == null)
                {
                    _peoplePath = Path.Combine(ImagesByNamePath, "People");
                    if (!Directory.Exists(_peoplePath))
                    {
                        Directory.CreateDirectory(_peoplePath);
                    }
                }

                return _peoplePath;
            }
        }

        /// <summary>
        /// The _genre path
        /// </summary>
        private string _genrePath;
        /// <summary>
        /// Gets the path to the Genre directory
        /// </summary>
        /// <value>The genre path.</value>
        public string GenrePath
        {
            get
            {
                if (_genrePath == null)
                {
                    _genrePath = Path.Combine(ImagesByNamePath, "Genre");
                    if (!Directory.Exists(_genrePath))
                    {
                        Directory.CreateDirectory(_genrePath);
                    }
                }

                return _genrePath;
            }
        }

        /// <summary>
        /// The _studio path
        /// </summary>
        private string _studioPath;
        /// <summary>
        /// Gets the path to the Studio directory
        /// </summary>
        /// <value>The studio path.</value>
        public string StudioPath
        {
            get
            {
                if (_studioPath == null)
                {
                    _studioPath = Path.Combine(ImagesByNamePath, "Studio");
                    if (!Directory.Exists(_studioPath))
                    {
                        Directory.CreateDirectory(_studioPath);
                    }
                }

                return _studioPath;
            }
        }

        /// <summary>
        /// The _year path
        /// </summary>
        private string _yearPath;
        /// <summary>
        /// Gets the path to the Year directory
        /// </summary>
        /// <value>The year path.</value>
        public string YearPath
        {
            get
            {
                if (_yearPath == null)
                {
                    _yearPath = Path.Combine(ImagesByNamePath, "Year");
                    if (!Directory.Exists(_yearPath))
                    {
                        Directory.CreateDirectory(_yearPath);
                    }
                }

                return _yearPath;
            }
        }

        /// <summary>
        /// The _general path
        /// </summary>
        private string _generalPath;
        /// <summary>
        /// Gets the path to the General IBN directory
        /// </summary>
        /// <value>The general path.</value>
        public string GeneralPath
        {
            get
            {
                if (_generalPath == null)
                {
                    _generalPath = Path.Combine(ImagesByNamePath, "General");
                    if (!Directory.Exists(_generalPath))
                    {
                        Directory.CreateDirectory(_generalPath);
                    }
                }

                return _generalPath;
            }
        }

        /// <summary>
        /// The _ratings path
        /// </summary>
        private string _ratingsPath;
        /// <summary>
        /// Gets the path to the Ratings IBN directory
        /// </summary>
        /// <value>The ratings path.</value>
        public string RatingsPath
        {
            get
            {
                if (_ratingsPath == null)
                {
                    _ratingsPath = Path.Combine(ImagesByNamePath, "Ratings");
                    if (!Directory.Exists(_ratingsPath))
                    {
                        Directory.CreateDirectory(_ratingsPath);
                    }
                }

                return _ratingsPath;
            }
        }

        /// <summary>
        /// The _user configuration directory path
        /// </summary>
        private string _userConfigurationDirectoryPath;
        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        /// <value>The user configuration directory path.</value>
        public string UserConfigurationDirectoryPath
        {
            get
            {
                if (_userConfigurationDirectoryPath == null)
                {
                    _userConfigurationDirectoryPath = Path.Combine(ConfigurationDirectoryPath, "users");
                    if (!Directory.Exists(_userConfigurationDirectoryPath))
                    {
                        Directory.CreateDirectory(_userConfigurationDirectoryPath);
                    }
                }
                return _userConfigurationDirectoryPath;
            }
        }

        /// <summary>
        /// The _f F MPEG stream cache path
        /// </summary>
        private string _fFMpegStreamCachePath;
        /// <summary>
        /// Gets the FF MPEG stream cache path.
        /// </summary>
        /// <value>The FF MPEG stream cache path.</value>
        public string EncodedMediaCachePath
        {
            get
            {
                if (_fFMpegStreamCachePath == null)
                {
                    _fFMpegStreamCachePath = Path.Combine(CachePath, "ffmpeg-streams");

                    if (!Directory.Exists(_fFMpegStreamCachePath))
                    {
                        Directory.CreateDirectory(_fFMpegStreamCachePath);
                    }
                }

                return _fFMpegStreamCachePath;
            }
        }

        /// <summary>
        /// The _images data path
        /// </summary>
        private string _downloadedImagesDataPath;
        /// <summary>
        /// Gets the images data path.
        /// </summary>
        /// <value>The images data path.</value>
        public string DownloadedImagesDataPath
        {
            get
            {
                if (_downloadedImagesDataPath == null)
                {
                    _downloadedImagesDataPath = Path.Combine(DataPath, "remote-images");

                    if (!Directory.Exists(_downloadedImagesDataPath))
                    {
                        Directory.CreateDirectory(_downloadedImagesDataPath);
                    }
                }

                return _downloadedImagesDataPath;
            }
        }
    }
}
