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
                    InitDirectories();  //move this here so we don't have to check for existence on every directory reference
                }
                return _programDataPath;
            }
        }

        private static void InitDirectories()
        {
            if (!Directory.Exists(LogDirectoryPath))
            {
                Directory.CreateDirectory(LogDirectoryPath);
            }
            if (!Directory.Exists(PluginsPath))
            {
                Directory.CreateDirectory(PluginsPath);
            }
            if (!Directory.Exists(RootFolderPath))
            {
                Directory.CreateDirectory(RootFolderPath);
            }
            if (!Directory.Exists(ConfigurationPath))
            {
                Directory.CreateDirectory(ConfigurationPath);
                Directory.CreateDirectory(SystemConfigurationPath);
                Directory.CreateDirectory(DeviceConfigurationPath);
                Directory.CreateDirectory(UserConfigurationPath);
            }


        }

        /// <summary>
        /// Gets the path to the plugin directory
        /// </summary>
        public static string PluginsPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "plugins");
            }
        }

        /// <summary>
        /// Gets the path to the application configuration root directory
        /// </summary>
        public static string ConfigurationPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "config");
            }
        }

        /// <summary>
        /// Gets the path to the system configuration directory
        /// </summary>
        public static string SystemConfigurationPath
        {
            get
            {
                return Path.Combine(ConfigurationPath, "system");
            }
        }

        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        public static string UserConfigurationPath
        {
            get
            {
                return Path.Combine(ConfigurationPath, "user");
            }
        }

        /// <summary>
        /// Gets the path to the device configuration directory
        /// </summary>
        public static string DeviceConfigurationPath
        {
            get
            {
                return Path.Combine(ConfigurationPath, "device");
            }
        }

        /// <summary>
        /// Gets the path to the log directory
        /// </summary>
        public static string LogDirectoryPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "logs");
            }
        }

        /// <summary>
        /// Gets the path to the root media directory
        /// </summary>
        public static string RootFolderPath
        {
            get
            {
                return Path.Combine(ProgramDataPath, "root");
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
