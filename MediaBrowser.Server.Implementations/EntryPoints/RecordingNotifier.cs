using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.EntryPoints
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

        public void Run()
        {
            _liveTvManager.TimerCancelled += _liveTvManager_TimerCancelled;
            _liveTvManager.SeriesTimerCancelled += _liveTvManager_SeriesTimerCancelled;
            _liveTvManager.TimerCreated += _liveTvManager_TimerCreated;
            _liveTvManager.SeriesTimerCreated += _liveTvManager_SeriesTimerCreated;
        }

        private void _liveTvManager_SeriesTimerCreated(object sender, Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCreated", e.Argument);
        }

        private void _liveTvManager_TimerCreated(object sender, Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCreated", e.Argument);
        }

        private void _liveTvManager_SeriesTimerCancelled(object sender, Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("SeriesTimerCancelled", e.Argument);
        }

        private void _liveTvManager_TimerCancelled(object sender, Model.Events.GenericEventArgs<TimerEventInfo> e)
        {
            SendMessage("TimerCancelled", e.Argument);
        }

        private async void SendMessage(string name, TimerEventInfo info)
        {
            var users = _userManager.Users.Where(i => i.Policy.EnableLiveTvAccess).ToList();

            foreach (var user in users)
            {
                try
                {
                    await _sessionManager.SendMessageToUserSessions<TimerEventInfo>(user.Id.ToString("N"), name, info, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending message", ex);
                }
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
