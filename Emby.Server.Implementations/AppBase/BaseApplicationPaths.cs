using System.IO;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.AppBase
{
    /// <summary>
    /// Provides a base class to hold common application paths used by both the Ui and Server.
    /// This can be subclassed to add application-specific paths.
    /// </summary>
    public abstract class BaseApplicationPaths : IApplicationPaths
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationPaths"/> class.
        /// </summary>
        protected BaseApplicationPaths(
            string programDataPath,
            string appFolderPath,
            string logDirectoryPath = null,
            string configurationDirectoryPath = null,
            string cacheDirectoryPath = null)
        {
            ProgramDataPath = programDataPath;
            ProgramSystemPath = appFolderPath;
            LogDirectoryPath = logDirectoryPath;
            ConfigurationDirectoryPath = configurationDirectoryPath;
            CachePath = cacheDirectoryPath;
        }

        public string ProgramDataPath { get; private set; }

        /// <summary>
        /// Gets the path to the system folder
        /// </summary>
        public string ProgramSystemPath { get; private set; }

        /// <summary>
        /// The _data directory
        /// </summary>
        private string _dataDirectory;
        /// <summary>
        /// Gets the folder path to the data directory
        /// </summary>
        /// <value>The data directory.</value>
        public string DataPath
        {
            get
            {
                if (_dataDirectory == null)
                {
                    _dataDirectory = Path.Combine(ProgramDataPath, "data");

                    Directory.CreateDirectory(_dataDirectory);
                }

                return _dataDirectory;
            }
        }

        private const string _virtualDataPath = "%AppDataPath%";
        public string VirtualDataPath => _virtualDataPath;

        /// <summary>
        /// Gets the image cache path.
        /// </summary>
        /// <value>The image cache path.</value>
        public string ImageCachePath => Path.Combine(CachePath, "images");

        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        /// <value>The plugins path.</value>
        public string PluginsPath => Path.Combine(ProgramDataPath, "plugins");

        /// <summary>
        /// Gets the path to the plugin configurations directory
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string PluginConfigurationsPath => Path.Combine(PluginsPath, "configurations");

        /// <summary>
        /// Gets the path to where temporary update files will be stored
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string TempUpdatePath => Path.Combine(ProgramDataPath, "updates");

        /// <summary>
        /// The _log directory
        /// </summary>
        private string _logDirectoryPath;

        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
        /// <value>The log directory path.</value>
        public string LogDirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(_logDirectoryPath))
                {
                    _logDirectoryPath = Path.Combine(ProgramDataPath, "logs");

                    Directory.CreateDirectory(_logDirectoryPath);
                }

                return _logDirectoryPath;
            }
            set => _logDirectoryPath = value;
        }

        /// <summary>
        /// The _config directory
        /// </summary>
        private string _configurationDirectoryPath;

        /// <summary>
        /// Gets the path to the application configuration root directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        public string ConfigurationDirectoryPath
        {
            get
            {
                if (string.IsNullOrEmpty(_configurationDirectoryPath))
                {
                    _configurationDirectoryPath = Path.Combine(ProgramDataPath, "config");

                    Directory.CreateDirectory(_configurationDirectoryPath);
                }

                return _configurationDirectoryPath;
            }
            set => _configurationDirectoryPath = value;
        }

        /// <summary>
        /// Gets the path to the system configuration file
        /// </summary>
        /// <value>The system configuration file path.</value>
        public string SystemConfigurationFilePath => Path.Combine(ConfigurationDirectoryPath, "system.xml");

        /// <summary>
        /// The _cache directory
        /// </summary>
        private string _cachePath;
        /// <summary>
        /// Gets the folder path to the cache directory
        /// </summary>
        /// <value>The cache directory.</value>
        public string CachePath
        {
            get
            {
                if (string.IsNullOrEmpty(_cachePath))
                {
                    _cachePath = Path.Combine(ProgramDataPath, "cache");

                    Directory.CreateDirectory(_cachePath);
                }

                return _cachePath;
            }
            set => _cachePath = value;
        }

        /// <summary>
        /// Gets the folder path to the temp directory within the cache folder
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory => Path.Combine(CachePath, "temp");
    }
}
