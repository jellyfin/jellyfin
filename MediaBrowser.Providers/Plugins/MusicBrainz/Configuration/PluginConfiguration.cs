#pragma warning disable CS1591

using MediaBrowser.Model.Plugins;
using MetaBrainz.MusicBrainz;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        private string _server = Plugin.DefaultServer;

        private double _rateLimit = Plugin.DefaultRateLimit;

        public string Server
        {
            get
            {
                return _server;
            }

            set
            {
                _server = value.TrimEnd('/');
                Query.DefaultWebSite = _server;
            }
        }

        public double RateLimit
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

                Query.DelayBetweenRequests = _rateLimit;
            }
        }

        public bool ReplaceArtistName { get; set; }
    }
}
