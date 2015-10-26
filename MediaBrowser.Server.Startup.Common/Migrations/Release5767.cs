using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Server.Implementations.LiveTv;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Implementations.ScheduledTasks;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class Release5767 : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly ITaskManager _taskManager;

        public Release5767(IServerConfigurationManager config, ITaskManager taskManager)
        {
            _config = config;
            _taskManager = taskManager;
        }

        public async void Run()
        {
            var name = "5767.1";

            if (_config.Configuration.Migrations.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            Task.Run(async () =>
            {
                await Task.Delay(3000).ConfigureAwait(false);

                _taskManager.QueueScheduledTask<CleanDatabaseScheduledTask>();
            });

            // Wait a few minutes before marking this as done. Make sure the server doesn't get restarted.
            await Task.Delay(300000).ConfigureAwait(false);
            
            var list = _config.Configuration.Migrations.ToList();
            list.Add(name);
            _config.Configuration.Migrations = list.ToArray();
            _config.SaveConfiguration();
        }
    }
}
