using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Channels
{
    /// <summary>
    /// The "Refresh Channels" scheduled task.
    /// </summary>
    public class RefreshChannelsScheduledTask : IScheduledTask, IConfigurableScheduledTask
    {
        private readonly IChannelManager _channelManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshChannelsScheduledTask"/> class.
        /// </summary>
        /// <param name="channelManager">The channel manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="localization">The localization manager.</param>
        public RefreshChannelsScheduledTask(
            IChannelManager channelManager,
            ILogger<RefreshChannelsScheduledTask> logger,
            ILibraryManager libraryManager,
            ILocalizationManager localization)
        {
            _channelManager = channelManager;
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

            await new ChannelPostScanTask(_channelManager, _logger, _libraryManager).Run(progress, cancellationToken)
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
