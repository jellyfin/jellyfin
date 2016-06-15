using System.Threading.Tasks;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Server.Implementations.Persistence;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class DbMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly ITaskManager _taskManager;

        public DbMigration(IServerConfigurationManager config, ITaskManager taskManager)
        {
            _config = config;
            _taskManager = taskManager;
        }

        public void Run()
        {
            // If a forced migration is required, do that now
            if (_config.Configuration.MigrationVersion < CleanDatabaseScheduledTask.MigrationVersion)
            {
                if (!_config.Configuration.IsStartupWizardCompleted)
                {
                    _config.Configuration.MigrationVersion = CleanDatabaseScheduledTask.MigrationVersion;
                    _config.SaveConfiguration();
                    return;
                }

                _taskManager.SuspendTriggers = true;
                CleanDatabaseScheduledTask.EnableUnavailableMessage = true;
                
                Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(false);

                    _taskManager.Execute<CleanDatabaseScheduledTask>();
                });

                return;
            }
            
            if (_config.Configuration.SchemaVersion < SqliteItemRepository.LatestSchemaVersion)
            {
                if (!_config.Configuration.IsStartupWizardCompleted)
                {
                    _config.Configuration.SchemaVersion = SqliteItemRepository.LatestSchemaVersion;
                    _config.SaveConfiguration();
                    return;
                }

                Task.Run(async () =>
                {
                    await Task.Delay(1000).ConfigureAwait(false);

                    _taskManager.Execute<CleanDatabaseScheduledTask>();
                });
            }
        }
    }
}
