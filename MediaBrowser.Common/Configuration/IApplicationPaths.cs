using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Common.Configuration
{
    /// <summary>
    /// Interface IApplicationPaths.
    /// </summary>
    public interface IApplicationPaths
    {
        /// <summary>
        /// Gets the path to the program data folder.
        /// </summary>
        /// <value>The program data path.</value>
        string ProgramDataPath { get; }

        /// <summary>
        /// Gets the path to the web UI resources folder.
        /// </summary>
        /// <remarks>
        /// This value is not relevant if the server is configured to not host any static web content. Additionally,
        /// the value for <see cref="ServerConfiguration.DashboardSourcePath"/> takes precedence over this one.
        /// </remarks>
        string WebPath { get; }

        /// <summary>
        /// Gets the path to the program system folder.
        /// </summary>
        /// <value>The program data path.</value>
        string ProgramSystemPath { get; }

        /// <summary>
        /// Gets the folder path to the data directory.
        /// </summary>
        /// <value>The data directory.</value>
        string DataPath { get; }

        /// <summary>
        /// Gets the image cache path.
        /// </summary>
        /// <value>The image cache path.</value>
        string ImageCachePath { get; }

        /// <summary>
        /// Gets the path to the plugin directory.
        /// </summary>
        /// <value>The plugins path.</value>
        string PluginsPath { get; }

        /// <summary>
        /// Gets the path to the plugin configurations directory.
        /// </summary>
        /// <value>The plugin configurations path.</value>
        string PluginConfigurationsPath { get; }

        /// <summary>
        /// Gets the path to the log directory.
        /// </summary>
        /// <value>The log directory path.</value>
        string LogDirectoryPath { get; }

        /// <summary>
        /// Gets the path to the application configuration root directory.
        /// </summary>
        /// <value>The configuration directory path.</value>
        string ConfigurationDirectoryPath { get; }

        /// <summary>
        /// Gets the path to the system configuration file.
        /// </summary>
        /// <value>The system configuration file path.</value>
        string SystemConfigurationFilePath { get; }

        /// <summary>
        /// Gets the folder path to the cache directory.
        /// </summary>
        /// <value>The cache directory.</value>
        string CachePath { get; }

        /// <summary>
        /// Gets the folder path to the temp directory within the cache folder.
        /// </summary>
        /// <value>The temp directory.</value>
        string TempDirectory { get; }

        /// <summary>
        /// Gets the magic string used for virtual path manipulation.
        /// </summary>
        /// <value>The magic string used for virtual path manipulation.</value>
        string VirtualDataPath { get; }
    }
}
