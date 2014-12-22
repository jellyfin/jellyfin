using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class MigrateTranscodingPath : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;

        public MigrateTranscodingPath(IServerConfigurationManager config)
        {
            _config = config;
        }

        public void Run()
        {
            if (!string.IsNullOrWhiteSpace(_config.Configuration.TranscodingTempPath))
            {
                var newConfig = _config.GetConfiguration<EncodingOptions>("encoding");

                newConfig.TranscodingTempPath = _config.Configuration.TranscodingTempPath;
                _config.SaveConfiguration("encoding", newConfig);

                _config.Configuration.TranscodingTempPath = null;
                _config.SaveConfiguration();
            }
        }
    }
}
