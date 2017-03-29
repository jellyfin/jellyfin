using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public string Name
        {
            get { return "Refresh Guide"; }
        }

        public string Description
        {
            get { return "Downloads channel information from live tv services."; }
        }

        public string Category
        {
            get { return "Live TV"; }
        }

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
            return new[] { 
            
                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(12).Ticks}
            };
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        public bool IsHidden
        {
            get { return _liveTvManager.Services.Count == 1 && GetConfiguration().TunerHosts.Count == 0; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }

        public bool IsLogged
        {
            get { return true; }
        }

        public string Key
        {
            get { return "RefreshGuide"; }
        }
    }
}
