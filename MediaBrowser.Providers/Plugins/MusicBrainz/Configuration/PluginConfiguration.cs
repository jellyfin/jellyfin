using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Server { get; set; } = "https://www.musicbrainz.org";

        public long RateLimit { get; set; } = 1000u;

        public bool Enable { get; set; }

        public bool ReplaceArtistName { get; set; }
    }
}
