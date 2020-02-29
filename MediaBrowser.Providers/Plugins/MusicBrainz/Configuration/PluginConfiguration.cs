using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _server = Plugin.Instance.DefaultServer;

        private long _rateLimit = Plugin.Instance.DefaultRateLimit;

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
                if (value < Plugin.Instance.DefaultRateLimit && _server == Plugin.Instance.DefaultServer)
                {
                    RateLimit = Plugin.Instance.DefaultRateLimit;
                }
            }
        }

        public bool Enable { get; set; }

        public bool ReplaceArtistName { get; set; }
    }
}
