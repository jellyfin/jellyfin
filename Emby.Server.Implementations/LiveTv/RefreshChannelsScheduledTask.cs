#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.LiveTv
{
    public class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IConfigurationManager _config;

        public RefreshChannelsScheduledTask(ILiveTvManager liveTvManager, IConfigurationManager config)
        {
            _liveTvManager = liveTvManager;
            _config = config;
        }

        public string Name => "Refresh Guide";

        public string Description => "Downloads channel information from live tv services.";

        public string Category => "Live TV";

        public Task Execute(System.Threading.CancellationToken cancellationToken, IProgress<double> progress)
        {
            var manager = (LiveTvManager)_liveTvManager;

            return manager.RefreshChannels(progress, cancellationToken);
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks}
            };
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        public bool IsHidden => _liveTvManager.Services.Count == 1 && GetConfiguration().TunerHosts.Length == 0;

        public bool IsEnabled => true;

        public bool IsLogged => true;

        public string Key => "RefreshGuide";
    }
}
