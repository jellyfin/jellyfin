using MediaBrowser.Common.Configuration;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Common.Implementations
{
    /// <summary>
    /// Provides a base class to hold common application paths used by both the Ui and Server.
    /// This can be subclassed to add application-specific paths.
    /// </summary>
    public abstract class BaseApplicationPaths : IApplicationPaths
    {
        /// <summary>
        /// The _use debug path
        /// </summary>
        private readonly bool _useDebugPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationPaths" /> class.
        /// </summary>
        /// <param name="useDebugPath">if set to <c>true</c> [use debug paths].</param>
        protected BaseApplicationPaths(bool useDebugPath)
        {
            _useDebugPath = useDebugPath;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationPaths"/> class.
        /// </summary>
        /// <param name="programDataPath">The program data path.</param>
        protected BaseApplicationPaths(string programDataPath)
        {
            _programDataPath = programDataPath;
        }

        /// <summary>
        /// The _program data path
        /// </summary>
        private string _programDataPath;
        /// <summary>
        /// Gets the path to the program data folder
        /// </summary>
        /// <value>The program data path.</value>
        public string ProgramDataPath
        {
            get
            {
                return _programDataPath ?? (_programDataPath = GetProgramDataPath());
            }
        }

        /// <summary>
        /// Gets the path to the system folder
        /// </summary>
        public string ProgramSystemPath { get { return Path.Combine(ProgramDataPath, "system"); } }

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

                    if (!Directory.Exists(_dataDirectory))
                    {
                        Directory.CreateDirectory(_dataDirectory);
                    }
                }

                return _dataDirectory;
            }
        }

        /// <summary>
        /// The _image cache path
        /// </summary>
        private string _imageCachePath;
        /// <summary>
        /// Gets the image cache path.
        /// </summary>
        /// <value>The image cache path.</value>
        public string ImageCachePath
        {
            get
            {
                if (_imageCachePath == null)
                {
                    _imageCachePath = Path.Combine(CachePath, "images");

                    if (!Directory.Exists(_imageCachePath))
                    {
                        Directory.CreateDirectory(_imageCachePath);
                    }
                }

                return _imageCachePath;
            }
        }

        /// <summary>
        /// The _plugins path
        /// </summary>
        private string _pluginsPath;
        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        /// <value>The plugins path.</value>
        public string PluginsPath
        {
            get
            {
                if (_pluginsPath == null)
                {
                    _pluginsPath = Path.Combine(ProgramDataPath, "plugins");
                    if (!Directory.Exists(_pluginsPath))
                    {
                        Directory.CreateDirectory(_pluginsPath);
                    }
                }

                return _pluginsPath;
            }
        }

        /// <summary>
        /// The _plugin configurations path
        /// </summary>
        private string _pluginConfigurationsPath;
        /// <summary>
        /// Gets the path to the plugin configurations directory
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string PluginConfigurationsPath
        {
            get
            {
                if (_pluginConfigurationsPath == null)
                {
                    _pluginConfigurationsPath = Path.Combine(PluginsPath, "configurations");
                    if (!Directory.Exists(_pluginConfigurationsPath))
                    {
                        Directory.CreateDirectory(_pluginConfigurationsPath);
                    }
                }

                return _pluginConfigurationsPath;
            }
        }

        private string _tempUpdatePath;
        /// <summary>
        /// Gets the path to where temporary update files will be stored
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string TempUpdatePath
        {
            get
            {
                if (_tempUpdatePath == null)
                {
                    _tempUpdatePath = Path.Combine(ProgramDataPath, "updates");
                    if (!Directory.Exists(_tempUpdatePath))
                    {
                        Directory.CreateDirectory(_tempUpdatePath);
                    }
                }

                return _tempUpdatePath;
            }
        }

        /// <summary>
        /// The _log directory path
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
                if (_logDirectoryPath == null)
                {
                    _logDirectoryPath = Path.Combine(ProgramDataPath, "logs");
                    if (!Directory.Exists(_logDirectoryPath))
                    {
                        Directory.CreateDirectory(_logDirectoryPath);
                    }
                }
                return _logDirectoryPath;
            }
        }

        /// <summary>
        /// The _configuration directory path
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
                if (_configurationDirectoryPath == null)
                {
                    _configurationDirectoryPath = Path.Combine(ProgramDataPath, "config");
                    if (!Directory.Exists(_configurationDirectoryPath))
                    {
                        Directory.CreateDirectory(_configurationDirectoryPath);
                    }
                }
                return _configurationDirectoryPath;
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
                if (_cachePath == null)
                {
                    _cachePath = Path.Combine(ProgramDataPath, "cache");

                    if (!Directory.Exists(_cachePath))
                    {
                        Directory.CreateDirectory(_cachePath);
                    }
                }

                return _cachePath;
            }
        }

        /// <summary>
        /// The _temp directory
        /// </summary>
        private string _tempDirectory;
        /// <summary>
        /// Gets the folder path to the temp directory within the cache folder
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory
        {
            get
            {
                if (_tempDirectory == null)
                {
                    _tempDirectory = Path.Combine(CachePath, "temp");

                    if (!Directory.Exists(_tempDirectory))
                    {
                        Directory.CreateDirectory(_tempDirectory);
                    }
                }

                return _tempDirectory;
            }
        }

        /// <summary>
        /// Gets the path to the application's ProgramDataFolder
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetProgramDataPath()
        {
            var programDataPath = _useDebugPath ? ConfigurationManager.AppSettings["DebugProgramDataPath"] : Path.Combine(ConfigurationManager.AppSettings["ReleaseProgramDataPath"], ConfigurationManager.AppSettings["ProgramDataFolderName"]);

            programDataPath = programDataPath.Replace("%ApplicationData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            // If it's a relative path, e.g. "..\"
            if (!Path.IsPathRooted(programDataPath))
            {
                var path = Assembly.GetExecutingAssembly().Location;
                path = Path.GetDirectoryName(path);

                if (string.IsNullOrEmpty(path))
                {
                    throw new ApplicationException("Unable to determine running assembly location");
                }

                programDataPath = Path.Combine(path, programDataPath);

                programDataPath = Path.GetFullPath(programDataPath);
            }

            if (!Directory.Exists(programDataPath))
            {
                Directory.CreateDirectory(programDataPath);
            }

            return programDataPath;
        }
    }
}
