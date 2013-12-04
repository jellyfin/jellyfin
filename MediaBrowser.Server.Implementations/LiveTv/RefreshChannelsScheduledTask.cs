using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv
{
    class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly ILiveTvManager _liveTvManager;

        public RefreshChannelsScheduledTask(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
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

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] 
            { 

                new StartupTrigger(),

                new SystemEventTrigger{ SystemEvent = SystemEvent.WakeFromSleep},

                new IntervalTrigger{ Interval = TimeSpan.FromHours(2)}
            };
        }

        public bool IsHidden
        {
            get { return _liveTvManager.ActiveService == null; }
        }
    }
}
