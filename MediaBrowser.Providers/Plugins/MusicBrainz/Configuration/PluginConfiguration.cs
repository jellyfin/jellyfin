using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool Enable { get; set; } = false;

        public bool ReplaceArtistName { get; set; } = false;

        public string Server { get; set; } = "https://www.musicbrainz.org";

        public long RateLimit { get; set; } = 1000u;
    }
}
