using System.Collections.Generic;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Emby.Server.Implementations
{
    public static class ConfigurationOptions
    {
        public static Dictionary<string, string> Configuration => new Dictionary<string, string>
        {
            { "HttpListenerHost:DefaultRedirectPath", "web/index.html" },
            { FfmpegProbeSizeKey, "1G" },
            { FfmpegAnalyzeDurationKey, "200M" }
        };
    }
}
