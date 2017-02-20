using System;
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
        protected BaseApplicationPaths(string programDataPath, string appFolderPath, Action<string> createDirectoryFn)
        {
            ProgramDataPath = programDataPath;
            ProgramSystemPath = appFolderPath;
            CreateDirectoryFn = createDirectoryFn;
        }

        protected Action<string> CreateDirectoryFn;

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

                    CreateDirectoryFn(_dataDirectory);
                }

                return _dataDirectory;
            }
        }

        /// <summary>
        /// Gets the image cache path.
        /// </summary>
        /// <value>The image cache path.</value>
        public string ImageCachePath
        {
            get
            {
                return Path.Combine(CachePath, "images");
            }
        }

        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        /// <value>The plugins path.</value>
        public string PluginsPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "plugins");
            }
        }

        /// <summary>
        /// Gets the path to the plugin configurations directory
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string PluginConfigurationsPath
        {
            get
            {
                return Path.Combine(PluginsPath, "configurations");
            }
        }

        /// <summary>
        /// Gets the path to where temporary update files will be stored
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string TempUpdatePath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "updates");
            }
        }

        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
        /// <value>The log directory path.</value>
        public string LogDirectoryPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "logs");
            }
        }

        /// <summary>
        /// Gets the path to the application configuration root directory
        /// </summary>
        /// <value>The configuration directory path.</value>
        public string ConfigurationDirectoryPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "config");
            }
        }

        /// <summary>
        /// Gets the path to the system configuration file
        /// </summary>
        /// <value>The system configuration file path.</value>
        public string SystemConfigurationFilePath
        {
            get
            {
                return Path.Combine(ConfigurationDirectoryPath, "system.xml");
            }
        }

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

                    CreateDirectoryFn(_cachePath);
                }

                return _cachePath;
            }
            set
            {
                _cachePath = value;
            }
        }

        /// <summary>
        /// Gets the folder path to the temp directory within the cache folder
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory
        {
            get
            {
                return Path.Combine(CachePath, "temp");
            }
        }
    }
}
