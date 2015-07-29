using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Channels
{
    class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public RefreshChannelsScheduledTask(IChannelManager channelManager, IUserManager userManager, ILogger logger)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
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

        public async Task Execute(System.Threading.CancellationToken cancellationToken, IProgress<double> progress)
        {
            var manager = (ChannelManager)_channelManager;

            await manager.RefreshChannels(new Progress<double>(), cancellationToken).ConfigureAwait(false);

            await new ChannelPostScanTask(_channelManager, _userManager, _logger).Run(progress, cancellationToken)
                    .ConfigureAwait(false);
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[] 
            { 
                new IntervalTrigger{ Interval = TimeSpan.FromHours(24)}
            };
        }

        public bool IsHidden
        {
            get { return false; }
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
