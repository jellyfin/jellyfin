using System.Collections.Generic;

namespace Emby.Server.Implementations
{
    public static class ConfigurationOptions
    {
        public static readonly Dictionary<string, string> Configuration = new Dictionary<string, string>
        {
            { "HttpListenerHost:DefaultRedirectPath", "web/index.html" },
            { "MusicBrainz:BaseUrl", "https://www.musicbrainz.org" }
        };
    }
}
