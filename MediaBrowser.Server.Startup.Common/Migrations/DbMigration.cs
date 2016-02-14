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
            if (_config.Configuration.MigrationVersion < CleanDatabaseScheduledTask.MigrationVersion && 
                _config.Configuration.IsStartupWizardCompleted)
            {
                _taskManager.SuspendTriggers = true;
                CleanDatabaseScheduledTask.EnableUnavailableMessage = true;
                
                Task.Run(async () =>
                {
                    await Task.Delay(100).ConfigureAwait(false);

                    _taskManager.Execute<CleanDatabaseScheduledTask>();
                });
            }
        }
    }
}
