using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Reflection;

namespace MediaBrowser.Common.Configuration
{
    public static class ApplicationPaths
    {
        private static string _programDataPath;
        /// <summary>
        /// Gets the path to the program data folder
        /// </summary>
        public static string ProgramDataPath
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


        private static string _pluginsPath;
        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        public static string PluginsPath
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

        private static string _configurationPath;
        /// <summary>
        /// Gets the path to the application configuration root directory
        /// </summary>
        public static string ConfigurationPath
        {
            get
            {
                if (_configurationPath == null)
                {
                    _configurationPath = Path.Combine(ProgramDataPath, "config");
                    if (!Directory.Exists(_configurationPath))
                    {
                        Directory.CreateDirectory(_configurationPath);
                    }
                }
                return _configurationPath;
            }
        }

        private static string _systemConfigurationPath;
        /// <summary>
        /// Gets the path to the system configuration directory
        /// </summary>
        public static string SystemConfigurationPath
        {
            get
            {
                if (_systemConfigurationPath == null)
                {
                    _systemConfigurationPath = Path.Combine(ConfigurationPath, "system");
                    if (!Directory.Exists(_systemConfigurationPath))
                    {
                        Directory.CreateDirectory(_systemConfigurationPath);
                    }
                }
                return _systemConfigurationPath;
            }
        }

        private static string _userConfigurationPath;
        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        public static string UserConfigurationPath
        {
            get
            {
                if (_userConfigurationPath == null)
                {
                    _userConfigurationPath = Path.Combine(ConfigurationPath, "user");
                    if (!Directory.Exists(_userConfigurationPath))
                    {
                        Directory.CreateDirectory(_userConfigurationPath);
                    }
                }
                return _userConfigurationPath;
            }
        }

        private static string _deviceConfigurationPath;
        /// <summary>
        /// Gets the path to the device configuration directory
        /// </summary>
        public static string DeviceConfigurationPath
        {
            get
            {
                if (_deviceConfigurationPath == null)
                {
                    _deviceConfigurationPath = Path.Combine(ConfigurationPath, "device");
                    if (!Directory.Exists(_deviceConfigurationPath))
                    {
                        Directory.CreateDirectory(_deviceConfigurationPath);
                    }
                }
                return _deviceConfigurationPath;
            }
        }

        private static string _logDirectoryPath;
        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
        public static string LogDirectoryPath
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

        private static string _rootFolderPath;
        /// <summary>
        /// Gets the path to the root media directory
        /// </summary>
        public static string RootFolderPath
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

        private static string _ibnPath;
        /// <summary>
        /// Gets the path to the Images By Name directory
        /// </summary>
        public static string IBNPath
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

                return _pluginsPath;
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
