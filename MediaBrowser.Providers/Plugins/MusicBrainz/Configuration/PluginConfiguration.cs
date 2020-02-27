using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string server = Plugin.Instance.DefaultServer;

        private long rateLimit = Plugin.Instance.DefaultRateLimit;

        public string Server
        {
            get
            {
                return server;
            }

            set
            {
                server = value.TrimEnd('/');
            }
        }

        public long RateLimit
        {
            get
            {
                return rateLimit;
            }

            set
            {
                if (value < 2000u && server == Plugin.Instance.DefaultServer)
                {
                    RateLimit = Plugin.Instance.DefaultRateLimit;
                }
            }
        }

        public bool Enable { get; set; }

        public bool ReplaceArtistName { get; set; }
    }
}
