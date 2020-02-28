using System.Collections.Generic;
using Emby.Server.Implementations.HttpServer;
using MediaBrowser.Providers.Music;
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
            { NoWebContentKey, bool.FalseString },
            { HttpListenerHost.DefaultRedirectKey, "web/index.html" },
            { MusicBrainzAlbumProvider.BaseUrlKey, "https://www.musicbrainz.org" },
            { FfmpegProbeSizeKey, "1G" },
            { FfmpegAnalyzeDurationKey, "200M" }
        };
    }
}
