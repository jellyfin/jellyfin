using System.IO;
using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller.Configuration
{
    public class ServerApplicationPaths : BaseApplicationPaths
    {
        private string _rootFolderPath;
        /// <summary>
        /// Gets the path to the root media directory
        /// </summary>
        public string RootFolderPath
        {
            get
            {
                if (_rootFolderPath == null)
                {
                    _rootFolderPath = Path.Combine(ProgramDataPath, "root");
                    if (!Directory.Exists(_rootFolderPath))
                    {
                        Directory.CreateDirectory(_rootFolderPath);
                    }
                }
                return _rootFolderPath;
            }
        }

        private string _ibnPath;
        /// <summary>
        /// Gets the path to the Images By Name directory
        /// </summary>
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

        private string _PeoplePath;
        /// <summary>
        /// Gets the path to the People directory
        /// </summary>
        public string PeoplePath
        {
            get
            {
                if (_PeoplePath == null)
                {
                    _PeoplePath = Path.Combine(ImagesByNamePath, "People");
                    if (!Directory.Exists(_PeoplePath))
                    {
                        Directory.CreateDirectory(_PeoplePath);
                    }
                }

                return _PeoplePath;
            }
        }

        private string _GenrePath;
        /// <summary>
        /// Gets the path to the Genre directory
        /// </summary>
        public string GenrePath
        {
            get
            {
                if (_GenrePath == null)
                {
                    _GenrePath = Path.Combine(ImagesByNamePath, "Genre");
                    if (!Directory.Exists(_GenrePath))
                    {
                        Directory.CreateDirectory(_GenrePath);
                    }
                }

                return _GenrePath;
            }
        }

        private string _StudioPath;
        /// <summary>
        /// Gets the path to the Studio directory
        /// </summary>
        public string StudioPath
        {
            get
            {
                if (_StudioPath == null)
                {
                    _StudioPath = Path.Combine(ImagesByNamePath, "Studio");
                    if (!Directory.Exists(_StudioPath))
                    {
                        Directory.CreateDirectory(_StudioPath);
                    }
                }

                return _StudioPath;
            }
        }

        private string _yearPath;
        /// <summary>
        /// Gets the path to the Year directory
        /// </summary>
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

        private string _userConfigurationDirectoryPath;
        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        public string UserConfigurationDirectoryPath
        {
            get
            {
                if (_userConfigurationDirectoryPath == null)
                {
                    _userConfigurationDirectoryPath = Path.Combine(ConfigurationDirectoryPath, "user");
                    if (!Directory.Exists(_userConfigurationDirectoryPath))
                    {
                        Directory.CreateDirectory(_userConfigurationDirectoryPath);
                    }
                }
                return _userConfigurationDirectoryPath;
            }
        }

    }
}
