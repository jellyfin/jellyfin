using System.Configuration;
using System.IO;
using System.Reflection;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Provides a base class to hold common application paths used by both the Ui and Server.
    /// This can be subclassed to add application-specific paths.
    /// </summary>
    public abstract class BaseApplicationPaths
    {
        private string _programDataPath;
        /// <summary>
        /// Gets the path to the program data folder
        /// </summary>
        public string ProgramDataPath
        {
            get
            {
                if (_programDataPath == null)
                {
                    _programDataPath = GetProgramDataPath();
                }
                
                return _programDataPath;
            }
        }

        private string _pluginsPath;
        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
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

        private string _pluginConfigurationsPath;
        /// <summary>
        /// Gets the path to the plugin configurations directory
        /// </summary>
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

        private string _logDirectoryPath;
        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
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

        private string _configurationDirectoryPath;
        /// <summary>
        /// Gets the path to the application configuration root directory
        /// </summary>
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

        private string _systemConfigurationFilePath;
        /// <summary>
        /// Gets the path to the system configuration file
        /// </summary>
        public string SystemConfigurationFilePath
        {
            get
            {
                if (_systemConfigurationFilePath == null)
                {
                    _systemConfigurationFilePath = Path.Combine(ConfigurationDirectoryPath, "system.xml");
                }
                return _systemConfigurationFilePath;
            }
        }

        /// <summary>
        /// Gets the path to the application's ProgramDataFolder
        /// </summary>
        private static string GetProgramDataPath()
        {
            string programDataPath = ConfigurationManager.AppSettings["ProgramDataPath"];

            // If it's a relative path, e.g. "..\"
            if (!Path.IsPathRooted(programDataPath))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                path = Path.GetDirectoryName(path);

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
