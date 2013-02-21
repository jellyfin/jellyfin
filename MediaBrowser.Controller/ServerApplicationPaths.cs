using System.IO;
using MediaBrowser.Common.Kernel;

namespace MediaBrowser.Controller
{
    /// <summary>
    /// Extends BaseApplicationPaths to add paths that are only applicable on the server
    /// </summary>
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

        private string _CacheDirectory;
        /// <summary>
        /// Gets the folder path to the cache directory
        /// </summary>
        public string CacheDirectory
        {
            get
            {
                if (_CacheDirectory == null)
                {
                    _CacheDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.ProgramDataPath, "cache");

                    if (!Directory.Exists(_CacheDirectory))
                    {
                        Directory.CreateDirectory(_CacheDirectory);
                    }
                }

                return _CacheDirectory;
            }
        }

        private string _FFProbeAudioCacheDirectory;
        /// <summary>
        /// Gets the folder path to the ffprobe audio cache directory
        /// </summary>
        public string FFProbeAudioCacheDirectory
        {
            get
            {
                if (_FFProbeAudioCacheDirectory == null)
                {
                    _FFProbeAudioCacheDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.CacheDirectory, "ffprobe-audio");

                    if (!Directory.Exists(_FFProbeAudioCacheDirectory))
                    {
                        Directory.CreateDirectory(_FFProbeAudioCacheDirectory);
                    }
                }

                return _FFProbeAudioCacheDirectory;
            }
        }

        private string _FFProbeVideoCacheDirectory;
        /// <summary>
        /// Gets the folder path to the ffprobe video cache directory
        /// </summary>
        public string FFProbeVideoCacheDirectory
        {
            get
            {
                if (_FFProbeVideoCacheDirectory == null)
                {
                    _FFProbeVideoCacheDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.CacheDirectory, "ffprobe-video");

                    if (!Directory.Exists(_FFProbeVideoCacheDirectory))
                    {
                        Directory.CreateDirectory(_FFProbeVideoCacheDirectory);
                    }
                }

                return _FFProbeVideoCacheDirectory;
            }
        }
        
        private string _FFMpegDirectory;
        /// <summary>
        /// Gets the folder path to ffmpeg
        /// </summary>
        public string FFMpegDirectory
        {
            get
            {
                if (_FFMpegDirectory == null)
                {
                    _FFMpegDirectory = Path.Combine(Kernel.Instance.ApplicationPaths.ProgramDataPath, "FFMpeg");

                    if (!Directory.Exists(_FFMpegDirectory))
                    {
                        Directory.CreateDirectory(_FFMpegDirectory);
                    }
                }

                return _FFMpegDirectory;
            }
        }

        private string _FFMpegPath;
        /// <summary>
        /// Gets the path to ffmpeg.exe
        /// </summary>
        public string FFMpegPath
        {
            get
            {
                if (_FFMpegPath == null)
                {
                    _FFMpegPath = Path.Combine(FFMpegDirectory, "ffmpeg.exe");
                }

                return _FFMpegPath;
            }
        }

        private string _FFProbePath;
        /// <summary>
        /// Gets the path to ffprobe.exe
        /// </summary>
        public string FFProbePath
        {
            get
            {
                if (_FFProbePath == null)
                {
                    _FFProbePath = Path.Combine(FFMpegDirectory, "ffprobe.exe");
                }

                return _FFProbePath;
            }
        }
    }
}
