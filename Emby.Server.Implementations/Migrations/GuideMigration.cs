using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Updates;
using System.Linq;

namespace Emby.Server.Implementations.Migrations
{
    public class GuideMigration : IVersionMigration
    {
        private readonly IServerConfigurationManager _config;
        private readonly ITaskManager _taskManager;

        public GuideMigration(IServerConfigurationManager config, ITaskManager taskManager)
        {
            _config = config;
            _taskManager = taskManager;
        }

        public async Task Run()
        {
            var name = "GuideRefresh2";

            if (!_config.Configuration.Migrations.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Task.Run(() =>
                {
                    _taskManager.QueueScheduledTask(_taskManager.ScheduledTasks.Select(i => i.ScheduledTask)
                            .First(i => string.Equals(i.Key, "RefreshGuide", StringComparison.OrdinalIgnoreCase)));
                });

                var list = _config.Configuration.Migrations.ToList();
                list.Add(name);
                _config.Configuration.Migrations = list.ToArray();
                _config.SaveConfiguration();
            }
        }
    }
}
