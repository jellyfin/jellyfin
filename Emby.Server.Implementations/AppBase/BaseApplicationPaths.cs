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

            DataPath = Directory.CreateDirectory(Path.Combine(ProgramDataPath, "data")).FullName;
        }

        /// <inheritdoc/>
        public string ProgramDataPath { get; }

        /// <inheritdoc/>
        public string WebPath { get; }

        /// <inheritdoc/>
        public string ProgramSystemPath { get; } = AppContext.BaseDirectory;

        /// <inheritdoc/>
        public string DataPath { get; }

        /// <inheritdoc />
        public string VirtualDataPath => "%AppDataPath%";

        /// <inheritdoc/>
        public string ImageCachePath => Path.Combine(CachePath, "images");

        /// <inheritdoc/>
        public string PluginsPath => Path.Combine(ProgramDataPath, "plugins");

        /// <inheritdoc/>
        public string PluginConfigurationsPath => Path.Combine(PluginsPath, "configurations");

        /// <inheritdoc/>
        public string LogDirectoryPath { get; }

        /// <inheritdoc/>
        public string ConfigurationDirectoryPath { get; }

        /// <inheritdoc/>
        public string SystemConfigurationFilePath => Path.Combine(ConfigurationDirectoryPath, "system.xml");

        /// <inheritdoc/>
        public string CachePath { get; set; }

        /// <inheritdoc/>
        public string TempDirectory => Path.Join(Path.GetTempPath(), "jellyfin");

        /// <inheritdoc />
        public string TrickplayPath => Path.Combine(DataPath, "trickplay");
    }
}
