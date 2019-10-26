using System.Collections.Generic;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Emby.Server.Implementations
{
    public static class ConfigurationOptions
    {
        public static Dictionary<string, string> Configuration => new Dictionary<string, string>
        {
            { "HttpListenerHost_DefaultRedirectPath", "web/index.html" },
            { "MusicBrainz_BaseUrl", "https://www.musicbrainz.org" },
            { FfmpegProbeSizeKey, "1G" },
            { FfmpegAnalyzeDuration, "200M" }
        };
    }
}
