#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Channels
{
    public class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        public RefreshChannelsScheduledTask(IChannelManager channelManager, IUserManager userManager, ILogger logger, ILibraryManager libraryManager)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public string Name => "Refresh Channels";

        /// <inheritdoc />
        public string Description => "Refreshes internet channel information.";

        /// <inheritdoc />
        public string Category => "Internet Channels";

        /// <inheritdoc />
        public bool IsHidden => ((ChannelManager)_channelManager).Channels.Length == 0;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;

        /// <inheritdoc />
        public string Key => "RefreshInternetChannels";

        /// <inheritdoc />
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var manager = (ChannelManager)_channelManager;

            await manager.RefreshChannels(new SimpleProgress<double>(), cancellationToken).ConfigureAwait(false);

            await new ChannelPostScanTask(_channelManager, _userManager, _logger, _libraryManager).Run(progress, cancellationToken)
                    .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {

                // Every so often
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks
                }
            };
        }
    }
}
