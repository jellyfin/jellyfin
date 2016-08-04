using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class CollectionsViewMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        public CollectionsViewMigration(IServerConfigurationManager config, IUserManager userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public void Run()
        {
            var migrationKey = this.GetType().Name;
            var migrationKeyList = _config.Configuration.Migrations.ToList();

            if (!migrationKeyList.Contains(migrationKey))
            {
                if (_config.Configuration.IsStartupWizardCompleted)
                {
                    if (_userManager.Users.Any(i => i.Configuration.DisplayCollectionsView))
                    {
                        _config.Configuration.DisplayCollectionsView = true;
                    }
                }

                migrationKeyList.Add(migrationKey);
                _config.Configuration.Migrations = migrationKeyList.ToArray();
                _config.SaveConfiguration();
            }

        }
    }
}
