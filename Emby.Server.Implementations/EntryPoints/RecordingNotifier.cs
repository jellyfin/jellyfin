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
    /// <summary>
    /// The recording notifier server entry point.
    /// </summary>
    public sealed class RecordingNotifier : IServerEntryPoint
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingNotifier"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="liveTvManager">The LiveTV manager.</param>
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

        private void OnLiveTvManagerSeriesTimerCreated(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCreated", e.Argument);
        }

        private void OnLiveTvManagerTimerCreated(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCreated", e.Argument);
        }

        private void OnLiveTvManagerSeriesTimerCancelled(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCancelled", e.Argument);
        }

        private void OnLiveTvManagerTimerCancelled(object sender, MediaBrowser.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCancelled", e.Argument);
        }

        private async void SendMessage(string name, TimerEventInfo info)
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
