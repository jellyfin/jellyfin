using System.Collections.Generic;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Emby.Server.Implementations
{
    /// <summary>
    /// Static class containing the default configuration options for the web server.
    /// </summary>
    public static class ConfigurationOptions
    {
        /// <summary>
        /// Gets the default configuration options.
        /// </summary>
        public static Dictionary<string, string> DefaultConfiguration => new Dictionary<string, string>
        {
            { "HttpListenerHost:DefaultRedirectPath", "web/index.html" },
            { "MusicBrainz:BaseUrl", "https://www.musicbrainz.org" },
            { FfmpegProbeSizeKey, "1G" },
            { FfmpegAnalyzeDurationKey, "200M" }
        };
    }
}
