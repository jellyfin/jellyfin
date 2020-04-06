using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _server = Plugin.DefaultServer;

        private long _rateLimit = Plugin.DefaultRateLimit;

        public string Server
        {
            get
            {
                return _server;
            }

            set
            {
                _server = value.TrimEnd('/');
            }
        }

        public long RateLimit
        {
            get
            {
                return _rateLimit;
            }

            set
            {
                if (value < Plugin.DefaultRateLimit && _server == Plugin.DefaultServer)
                {
                    _rateLimit = Plugin.DefaultRateLimit;
                }
                else
                {
                    _rateLimit = value;
                }
            }
        }

        public bool Enable { get; set; }

        public bool ReplaceArtistName { get; set; }
    }
}
