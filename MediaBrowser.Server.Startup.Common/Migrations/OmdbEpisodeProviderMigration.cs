using MediaBrowser.Controller.Configuration;
using System.Linq;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    class OmdbEpisodeProviderMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private const string _providerName = "The Open Movie Database";

        public OmdbEpisodeProviderMigration(IServerConfigurationManager config)
        {
            _config = config;
        }

        public void Run()
        {
            var migrationKey = this.GetType().FullName;
            var migrationKeyList = _config.Configuration.Migrations.ToList();

            if (!migrationKeyList.Contains(migrationKey))
            {
                foreach (var metaDataOption in _config.Configuration.MetadataOptions)
                {
                    if (metaDataOption.ItemType == "Episode")
                    {
                        var disabledFetchers = metaDataOption.DisabledMetadataFetchers.ToList();
                        if (!disabledFetchers.Contains(_providerName))
                        {
                            disabledFetchers.Add(_providerName);
                            metaDataOption.DisabledMetadataFetchers = disabledFetchers.ToArray();
                        }
                    }
                }

                migrationKeyList.Add(migrationKey);
                _config.Configuration.Migrations = migrationKeyList.ToArray();
                _config.SaveConfiguration();
            }

        }
    }
}
