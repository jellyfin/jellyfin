using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.LiveTv;
using Jellyfin.Controller.Plugins;
using Jellyfin.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.EntryPoints
{
    public class RecordingNotifier : IServerEntryPoint
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly ISessionManager _sessionManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public RecordingNotifier(ISessionManager sessionManager, IUserManager userManager, ILogger logger, ILiveTvManager liveTvManager)
        {
            _sessionManager = sessionManager;
            _userManager = userManager;
            _logger = logger;
            _liveTvManager = liveTvManager;
        }

        public Task RunAsync()
        {
            _liveTvManager.TimerCancelled += _liveTvManager_TimerCancelled;
            _liveTvManager.SeriesTimerCancelled += _liveTvManager_SeriesTimerCancelled;
            _liveTvManager.TimerCreated += _liveTvManager_TimerCreated;
            _liveTvManager.SeriesTimerCreated += _liveTvManager_SeriesTimerCreated;

            return Task.CompletedTask;
        }

        private void _liveTvManager_SeriesTimerCreated(object sender, Jellyfin.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCreated", e.Argument);
        }

        private void _liveTvManager_TimerCreated(object sender, Jellyfin.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCreated", e.Argument);
        }

        private void _liveTvManager_SeriesTimerCancelled(object sender, Jellyfin.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCancelled", e.Argument);
        }

        private void _liveTvManager_TimerCancelled(object sender, Jellyfin.Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCancelled", e.Argument);
        }

        private async void SendMessage(string name, TimerEventInfo info)
        {
            var users = _userManager.Users.Where(i => i.Policy.EnableLiveTvAccess).Select(i => i.Id).ToList();

            try
            {
                await _sessionManager.SendMessageToUserSessions(users, name, info, CancellationToken.None);
            }
            catch (ObjectDisposedException)
            {
                // TODO Log exception or Investigate and properly fix.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
            }
        }

        public void Dispose()
        {
            _liveTvManager.TimerCancelled -= _liveTvManager_TimerCancelled;
            _liveTvManager.SeriesTimerCancelled -= _liveTvManager_SeriesTimerCancelled;
            _liveTvManager.TimerCreated -= _liveTvManager_TimerCreated;
            _liveTvManager.SeriesTimerCreated -= _liveTvManager_SeriesTimerCreated;
        }
    }
}
