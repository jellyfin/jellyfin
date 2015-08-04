using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EntryPoint : IServerEntryPoint
    {
        private readonly IConfigurationManager _config;
        private readonly ISecurityManager _manager;

        public EntryPoint(IConfigurationManager config, ISecurityManager manager)
        {
            _config = config;
            _manager = manager;
        }

        public async void Run()
        {
            EmbyTV.Current.Start();

            if (GetConfiguration().ListingProviders.Count > 0 || GetConfiguration().TunerHosts.Count > 0)
            {
                try
                {
                    await _manager.GetRegistrationStatus("livetvguide").ConfigureAwait(false);
                }
                catch
                {
                    
                }
            }
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        public void Dispose()
        {
        }
    }
}
