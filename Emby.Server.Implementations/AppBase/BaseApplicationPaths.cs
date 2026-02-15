using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
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

        /// <inheritdoc />
        public string BackupPath => Path.Combine(DataPath, "backups");

        /// <inheritdoc />
        public virtual void MakeSanityCheckOrThrow()
        {
            CreateAndCheckMarker(ConfigurationDirectoryPath, "config");
            CreateAndCheckMarker(LogDirectoryPath, "log");
            CreateAndCheckMarker(PluginsPath, "plugin");
            CreateAndCheckMarker(ProgramDataPath, "data");
            CreateAndCheckMarker(CachePath, "cache");
            CreateAndCheckMarker(DataPath, "data");
        }

        /// <inheritdoc />
        public void CreateAndCheckMarker(string path, string markerName, bool recursive = false)
        {
            Directory.CreateDirectory(path);

            CheckOrCreateMarker(path, $".jellyfin-{markerName}", recursive);
        }

        private IEnumerable<string> GetMarkers(string path, bool recursive = false)
        {
            return Directory.EnumerateFiles(path, ".jellyfin-*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        private void CheckOrCreateMarker(string path, string markerName, bool recursive = false)
        {
            string? otherMarkers = null;
            try
            {
                otherMarkers = GetMarkers(path, recursive).FirstOrDefault(e => !Path.GetFileName(e.AsSpan()).Equals(markerName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                // Error while checking for marker files, assume none exist and keep going
                // TODO: add some logging
            }

            if (otherMarkers is not null)
            {
                throw new InvalidOperationException($"Expected to find only {markerName} but found marker for {otherMarkers}.");
            }

            var markerPath = Path.Combine(path, markerName);
            if (!File.Exists(markerPath))
            {
                FileHelper.CreateEmpty(markerPath);
            }
        }
    }
}
