using System;
using System.Collections.Generic;
using CommandLine;
using Emby.Server.Implementations;
using Emby.Server.Implementations.EntryPoints;
using Emby.Server.Implementations.Udp;
using Emby.Server.Implementations.Updates;
using MediaBrowser.Controller.Extensions;

namespace Jellyfin.Server
{
    /// <summary>
    /// Class used by CommandLine package when parsing the command line arguments.
    /// </summary>
    public class StartupOptions : IStartupOptions
    {
        /// <summary>
        /// Gets or sets the path to the data directory.
        /// </summary>
        /// <value>The path to the data directory.</value>
        [Option('d', "datadir", Required = false, HelpText = "Path to use for the data folder (database files, etc.).")]
        public string? DataDir { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server should not host the web client.
        /// </summary>
        [Option("nowebclient", Required = false, HelpText = "Indicates that the web server should not host the web client.")]
        public bool NoWebClient { get; set; }

        /// <summary>
        /// Gets or sets the path to the web directory.
        /// </summary>
        /// <value>The path to the web directory.</value>
        [Option('w', "webdir", Required = false, HelpText = "Path to the Jellyfin web UI resources.")]
        public string? WebDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the cache directory.
        /// </summary>
        /// <value>The path to the cache directory.</value>
        [Option('C', "cachedir", Required = false, HelpText = "Path to use for caching.")]
        public string? CacheDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the config directory.
        /// </summary>
        /// <value>The path to the config directory.</value>
        [Option('c', "configdir", Required = false, HelpText = "Path to use for configuration data (user settings and pictures).")]
        public string? ConfigDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the log directory.
        /// </summary>
        /// <value>The path to the log directory.</value>
        [Option('l', "logdir", Required = false, HelpText = "Path to use for writing log files.")]
        public string? LogDir { get; set; }

        /// <inheritdoc />
        [Option("ffmpeg", Required = false, HelpText = "Path to external FFmpeg executable to use in place of default found in PATH.")]
        public string? FFmpegPath { get; set; }

        /// <inheritdoc />
        [Option("service", Required = false, HelpText = "Run as headless service.")]
        public bool IsService { get; set; }

        /// <inheritdoc />
        [Option("noautorunwebapp", Required = false, HelpText = "Run headless if startup wizard is complete.")]
        public bool NoAutoRunWebApp { get; set; }

        /// <inheritdoc />
        [Option("package-name", Required = false, HelpText = "Used when packaging Jellyfin (example, synology).")]
        public string? PackageName { get; set; }

        /// <inheritdoc />
        [Option("restartpath", Required = false, HelpText = "Path to restart script.")]
        public string? RestartPath { get; set; }

        /// <inheritdoc />
        [Option("restartargs", Required = false, HelpText = "Arguments for restart script.")]
        public string? RestartArgs { get; set; }

        /// <inheritdoc />
        [Option("plugin-manifest-url", Required = false, HelpText = "A custom URL for the plugin repository JSON manifest")]
        public string? PluginManifestUrl { get; set; }

        /// <inheritdoc />
        [Option("published-server-url", Required = false, HelpText = "Jellyfin Server URL to publish via auto discover process")]
        public Uri? PublishedServerUrl { get; set; }

        /// <summary>
        /// Gets the command line options as a dictionary that can be used in the .NET configuration system.
        /// </summary>
        /// <returns>The configuration dictionary.</returns>
        public Dictionary<string, string> ConvertToConfig()
        {
            var config = new Dictionary<string, string>();

            if (PluginManifestUrl != null)
            {
                config.Add(InstallationManager.PluginManifestUrlKey, PluginManifestUrl);
            }

            if (NoWebClient)
            {
                config.Add(ConfigurationExtensions.HostWebClientKey, bool.FalseString);
            }

            if (PublishedServerUrl != null)
            {
                config.Add(UdpServer.AddressOverrideConfigKey, PublishedServerUrl.ToString());
            }

            return config;
        }
    }
}
