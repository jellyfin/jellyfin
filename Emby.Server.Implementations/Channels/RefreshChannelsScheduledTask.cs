#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.Channels
{
    public class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _channelManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        public RefreshChannelsScheduledTask(
            IChannelManager channelManager,
            IUserManager userManager,
            ILogger<RefreshChannelsScheduledTask> logger,
            ILibraryManager libraryManager,
            ILocalizationManager localization)
        {
            _channelManager = channelManager;
            _userManager = userManager;
            _logger = logger;
            _libraryManager = libraryManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TasksRefreshChannels");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TasksRefreshChannelsDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksChannelsCategory");

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
