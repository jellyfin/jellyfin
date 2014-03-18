using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _manager;

        public RefreshChannelsScheduledTask(IChannelManager manager)
        {
            _manager = manager;
        }

        public string Name
        {
            get { return "Refresh Channels"; }
        }

        public string Description
        {
            get { return "Refreshes internet channel information."; }
        }

        public string Category
        {
            get { return "Channels"; }
        }

        public Task Execute(System.Threading.CancellationToken cancellationToken, IProgress<double> progress)
        {
            var manager = (ChannelManager)_manager;

            return manager.RefreshChannels(progress, cancellationToken);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] 
            { 
                new StartupTrigger(),

                new SystemEventTrigger{ SystemEvent = SystemEvent.WakeFromSleep},

                new IntervalTrigger{ Interval = TimeSpan.FromHours(24)}
            };
        }

        public bool IsHidden
        {
            get { return true; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
