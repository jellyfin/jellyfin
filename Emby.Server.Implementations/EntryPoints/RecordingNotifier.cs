#pragma warning disable CS1591

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    public sealed class RecordingNotifier : IServerEntryPoint
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public RecordingNotifier(
            ISessionManager sessionManager,
            IUserManager userManager,
            ILogger<RecordingNotifier> logger,
            ILiveTvManager liveTvManager)
        {
            _sessionManager = sessionManager;
            _userManager = userManager;
            _logger = logger;
            _liveTvManager = liveTvManager;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _liveTvManager.TimerCancelled += OnLiveTvManagerTimerCancelled;
            _liveTvManager.SeriesTimerCancelled += OnLiveTvManagerSeriesTimerCancelled;
            _liveTvManager.TimerCreated += OnLiveTvManagerTimerCreated;
            _liveTvManager.SeriesTimerCreated += OnLiveTvManagerSeriesTimerCreated;

            return Task.CompletedTask;
        }

        private async void OnLiveTvManagerSeriesTimerCreated(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            await SendMessage("SeriesTimerCreated", e.Argument).ConfigureAwait(false);
        }

        private async void OnLiveTvManagerTimerCreated(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            await SendMessage("TimerCreated", e.Argument).ConfigureAwait(false);
        }

        private async void OnLiveTvManagerSeriesTimerCancelled(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            await SendMessage("SeriesTimerCancelled", e.Argument).ConfigureAwait(false);
        }

        private async void OnLiveTvManagerTimerCancelled(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            await SendMessage("TimerCancelled", e.Argument).ConfigureAwait(false);
        }

        private async Task SendMessage(string name, TimerEventInfo info)
        {
            var users = _userManager.Users.Where(i => i.Policy.EnableLiveTvAccess).Select(i => i.Id).ToList();

            try
            {
                await _sessionManager.SendMessageToUserSessions(users, name, info, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _liveTvManager.TimerCancelled -= OnLiveTvManagerTimerCancelled;
            _liveTvManager.SeriesTimerCancelled -= OnLiveTvManagerSeriesTimerCancelled;
            _liveTvManager.TimerCreated -= OnLiveTvManagerTimerCreated;
            _liveTvManager.SeriesTimerCreated -= OnLiveTvManagerSeriesTimerCreated;
        }
    }
}
