using System.Collections.Generic;
using System.CommandLine;
using Emby.Server.Implementations;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Jellyfin.Server
{
    /// <summary>
    /// Class used by when parsing the command line arguments for startup.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "SA1201:A field should not follow a property", Justification = "It is much easier to read this way since they work by pair.")]
    public class StartupOptions : Options, IStartupOptions
    {
        /// <summary>
        /// Setup the options needed for Jellyfin server mode.
        /// </summary>
        /// <param name="cmd">The <see cref="RootCommand"/> or <see cref="Command"/> to add the arguments to.</param>
        public static void Setup(Command cmd)
        {
            Options.Setup(cmd, typeof(StartupOptions));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupOptions"/> class.
        /// </summary>
        /// <param name="parseResult">Instance of the <see cref="ParseResult"/> interface.</param>
        public StartupOptions(ParseResult parseResult) : base(parseResult)
        {
        }

        /// <summary>
        /// Gets the path to the data directory.
        /// </summary>
        /// <value>The path to the data directory.</value>
        public string? DataDir
        {
            get { return ParseResult.GetValue(dataDirOption); }
        }

        private static Option<string> dataDirOption = new("--datadir", "-d")
        {
            Description = "Path to use for the data folder (database files, etc.)."
        };

        /// <summary>
        /// Gets a value indicating whether the server should not host the web client.
        /// </summary>
        public bool NoWebClient
        {
            get { return ParseResult.GetValue(noWebClientOption); }
        }

        private static Option<bool> noWebClientOption = new("--nowebclient")
        {
            Description = "Indicates that the web server should not host the web client."
        };

        /// <summary>
        /// Gets the path to the web directory.
        /// </summary>
        /// <value>The path to the web directory.</value>
        public string? WebDir
        {
            get { return ParseResult.GetValue(webDirOption); }
        }

        private static Option<string> webDirOption = new("--webdir", "-w")
        {
            Description = "Path to the Jellyfin web UI resources."
        };

        /// <summary>
        /// Gets the path to the cache directory.
        /// </summary>
        /// <value>The path to the cache directory.</value>
        public string? CacheDir
        {
            get { return ParseResult.GetValue(cacheDirOption); }
        }

        private static Option<string> cacheDirOption = new("--cachedir", "-C")
        {
            Description = "Path to use for caching."
        };

        /// <summary>
        /// Gets the path to the config directory.
        /// </summary>
        /// <value>The path to the config directory.</value>
        public string? ConfigDir
        {
            get { return ParseResult.GetValue(configDirOption); }
        }

        private static Option<string> configDirOption = new("--configdir", "-c")
        {
            Description = "Path to use for configuration data (user settings and pictures)."
        };

        /// <summary>
        /// Gets the path to the log directory.
        /// </summary>
        /// <value>The path to the log directory.</value>
        public string? LogDir
        {
            get { return ParseResult.GetValue(logDirOption); }
        }

        private static Option<string> logDirOption = new("--logdir", "-l")
        {
            Description = "Path to use for writing log files."
        };

        /// <inheritdoc />
        public string? FFmpegPath
        {
            get { return ParseResult.GetValue(ffmpegOption); }
        }

        private static Option<string> ffmpegOption = new("--ffmpeg")
        {
            Description = "Path to external FFmpeg executable to use in place of default found in PATH."
        };

        /// <inheritdoc />
        public bool IsService
        {
            get { return ParseResult.GetValue(isServiceOption); }
        }

        private static Option<bool> isServiceOption = new("--service")
        {
            Description = "Run as headless service."
        };

        /// <inheritdoc />
        public string? PackageName
        {
            get { return ParseResult.GetValue(packageNameOption); }
        }

        private static Option<string> packageNameOption = new("--packageName")
        {
            Description = "Used when packaging Jellyfin (example, synology)."
        };

        /// <inheritdoc />
        public string? PublishedServerUrl
        {
            get { return ParseResult.GetValue(publishedServerUrlOption); }
        }

        private static Option<string> publishedServerUrlOption = new("--published-server-url")
        {
            Description = "Jellyfin Server URL to publish via auto discover process."
        };

        /// <summary>
        /// Gets a value indicating whether the server should not detect network status change.
        /// </summary>
        public bool NoDetectNetworkChange
        {
            get { return ParseResult.GetValue(noNetChangeOption); }
        }

        private static Option<bool> noNetChangeOption = new("--nonetchange")
        {
            Description = "Indicates that the server should not detect network status change."
        };

        /// <summary>
        /// Gets the path to an jellyfin backup archive to restore the application to.
        /// </summary>
        public string? RestoreArchive
        {
            get { return ParseResult.GetValue(restoreArchiveOption); }
        }

        private Option<string> restoreArchiveOption = new("--restore-archive")
        {
            Description = "Path to a Jellyfin backup archive to restore from."
        };

        /// <summary>
        /// Gets the command line options as a dictionary that can be used in the .NET configuration system.
        /// </summary>
        /// <returns>The configuration dictionary.</returns>
        public Dictionary<string, string?> ConvertToConfig()
        {
            var config = new Dictionary<string, string?>();

            if (NoWebClient)
            {
                config.Add(HostWebClientKey, bool.FalseString);
            }

            if (PublishedServerUrl is not null)
            {
                config.Add(AddressOverrideKey, PublishedServerUrl);
            }

            if (FFmpegPath is not null)
            {
                config.Add(FfmpegPathKey, FFmpegPath);
            }

            if (NoDetectNetworkChange)
            {
                config.Add(DetectNetworkChangeKey, bool.FalseString);
            }

            return config;
        }
    }
}
