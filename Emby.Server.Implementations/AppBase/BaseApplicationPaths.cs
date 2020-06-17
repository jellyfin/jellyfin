using System;
using System.IO;
using MediaBrowser.Common.Configuration;

namespace Emby.Server.Implementations.AppBase
{
    /// <summary>
    /// Provides a base class to hold common application paths used by both the UI and Server.
    /// This can be subclassed to add application-specific paths.
    /// </summary>
    public abstract class BaseApplicationPaths : IApplicationPaths
    {
        private string _dataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationPaths"/> class.
        /// </summary>
        /// <param name="programDataPath">The program data path.</param>
        /// <param name="logDirectoryPath">The log directory path.</param>
        /// <param name="configurationDirectoryPath">The configuration directory path.</param>
        /// <param name="cacheDirectoryPath">The cache directory path.</param>
        /// <param name="webDirectoryPath">The web directory path.</param>
        protected BaseApplicationPaths(
            string programDataPath,
            string logDirectoryPath,
            string configurationDirectoryPath,
            string cacheDirectoryPath,
            string webDirectoryPath)
        {
            ProgramDataPath = programDataPath;
            LogDirectoryPath = logDirectoryPath;
            ConfigurationDirectoryPath = configurationDirectoryPath;
            CachePath = cacheDirectoryPath;
            WebPath = webDirectoryPath;

            DataPath = Path.Combine(ProgramDataPath, "data");
        }

        /// <summary>
        /// Gets the path to the program data folder.
        /// </summary>
        /// <value>The program data path.</value>
        public string ProgramDataPath { get; }

        /// <inheritdoc/>
        public string WebPath { get; }

        /// <summary>
        /// Gets the path to the system folder.
        /// </summary>
        /// <value>The path to the system folder.</value>
        public string ProgramSystemPath { get; } = AppContext.BaseDirectory;

        /// <summary>
        /// Gets the folder path to the data directory.
        /// </summary>
        /// <value>The data directory.</value>
        public string DataPath
        {
            get => _dataPath;
            private set => _dataPath = Directory.CreateDirectory(value).FullName;
        }

        /// <inheritdoc />
        public string VirtualDataPath { get; } = "%AppDataPath%";

        /// <summary>
        /// Gets the image cache path.
        /// </summary>
        /// <value>The image cache path.</value>
        public string ImageCachePath => Path.Combine(CachePath, "images");

        /// <summary>
        /// Gets the path to the plugin directory.
        /// </summary>
        /// <value>The plugins path.</value>
        public string PluginsPath => Path.Combine(ProgramDataPath, "plugins");

        /// <summary>
        /// Gets the path to the plugin configurations directory.
        /// </summary>
        /// <value>The plugin configurations path.</value>
        public string PluginConfigurationsPath => Path.Combine(PluginsPath, "configurations");

        /// <summary>
        /// Gets the path to the log directory.
        /// </summary>
        /// <value>The log directory path.</value>
        public string LogDirectoryPath { get; }

        /// <summary>
        /// Gets the path to the application configuration root directory.
        /// </summary>
        /// <value>The configuration directory path.</value>
        public string ConfigurationDirectoryPath { get; }

        /// <summary>
        /// Gets the path to the system configuration file.
        /// </summary>
        /// <value>The system configuration file path.</value>
        public string SystemConfigurationFilePath => Path.Combine(ConfigurationDirectoryPath, "system.xml");

        /// <summary>
        /// Gets or sets the folder path to the cache directory.
        /// </summary>
        /// <value>The cache directory.</value>
        public string CachePath { get; set; }

        /// <summary>
        /// Gets the folder path to the temp directory within the cache folder.
        /// </summary>
        /// <value>The temp directory.</value>
        public string TempDirectory => Path.Combine(CachePath, "temp");
    }
}
