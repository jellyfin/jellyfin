using System.Collections.Generic;
using Emby.Server.Implementations.HttpServer;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Static class containing the default configuration options for the web server.
    /// </summary>
    public static class ConfigurationOptions
    {
        /// <summary>
        /// Gets a new copy of the default configuration options.
        /// </summary>
        public static Dictionary<string, string> DefaultConfiguration => new Dictionary<string, string>
        {
            { HostWebClientKey, bool.TrueString },
            { DefaultRedirectKey, "web/index.html" },
            { FfmpegProbeSizeKey, "1G" },
            { FfmpegAnalyzeDurationKey, "200M" },
            { PlaylistsAllowDuplicatesKey, bool.FalseString },
            { BindToUnixSocketKey, bool.FalseString }
        };
    }
}
