using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Recordings
{
    /// <summary>
    /// <see cref="IHostedService"/> responsible for notifying users when a LiveTV recording is completed.
    /// </summary>
    public sealed class RecordingNotifier : IHostedService
    {
        private readonly ILogger<RecordingNotifier> _logger;
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILiveTvManager _liveTvManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingNotifier"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
        /// <param name="userManager">The <see cref="IUserManager"/>.</param>
        /// <param name="liveTvManager">The <see cref="ILiveTvManager"/>.</param>
        public RecordingNotifier(
            ILogger<RecordingNotifier> logger,
            ISessionManager sessionManager,
            IUserManager userManager,
            ILiveTvManager liveTvManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;
            _userManager = userManager;
            _liveTvManager = liveTvManager;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _liveTvManager.TimerCancelled += OnLiveTvManagerTimerCancelled;
            _liveTvManager.SeriesTimerCancelled += OnLiveTvManagerSeriesTimerCancelled;
            _liveTvManager.TimerCreated += OnLiveTvManagerTimerCreated;
            _liveTvManager.SeriesTimerCreated += OnLiveTvManagerSeriesTimerCreated;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _liveTvManager.TimerCancelled -= OnLiveTvManagerTimerCancelled;
            _liveTvManager.SeriesTimerCancelled -= OnLiveTvManagerSeriesTimerCancelled;
            _liveTvManager.TimerCreated -= OnLiveTvManagerTimerCreated;
            _liveTvManager.SeriesTimerCreated -= OnLiveTvManagerSeriesTimerCreated;

            return Task.CompletedTask;
        }

        private async void OnLiveTvManagerSeriesTimerCreated(object? sender, GenericEventArgs<TimerEventInfo> e)
            => await SendMessage(SessionMessageType.SeriesTimerCreated, e.Argument).ConfigureAwait(false);

        private async void OnLiveTvManagerTimerCreated(object? sender, GenericEventArgs<TimerEventInfo> e)
            => await SendMessage(SessionMessageType.TimerCreated, e.Argument).ConfigureAwait(false);

        private async void OnLiveTvManagerSeriesTimerCancelled(object? sender, GenericEventArgs<TimerEventInfo> e)
            => await SendMessage(SessionMessageType.SeriesTimerCancelled, e.Argument).ConfigureAwait(false);

        private async void OnLiveTvManagerTimerCancelled(object? sender, GenericEventArgs<TimerEventInfo> e)
            => await SendMessage(SessionMessageType.TimerCancelled, e.Argument).ConfigureAwait(false);

        private async Task SendMessage(SessionMessageType name, TimerEventInfo info)
        {
            var users = _userManager.Users
                .Where(i => i.HasPermission(PermissionKind.EnableLiveTvAccess))
                .Select(i => i.Id)
                .ToList();

            try
            {
                await _sessionManager.SendMessageToUserSessions(users, name, info, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }
    }
}
