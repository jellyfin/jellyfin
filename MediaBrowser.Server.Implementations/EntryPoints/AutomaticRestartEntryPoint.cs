using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class AutomaticRestartEntryPoint : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly ITaskManager _iTaskManager;
        private readonly ISessionManager _sessionManager;
        private readonly IServerConfigurationManager _config;
        private readonly ILiveTvManager _liveTvManager;

        private Timer _timer;

        public AutomaticRestartEntryPoint(IServerApplicationHost appHost, ILogger logger, ITaskManager iTaskManager, ISessionManager sessionManager, IServerConfigurationManager config, ILiveTvManager liveTvManager)
        {
            _appHost = appHost;
            _logger = logger;
            _iTaskManager = iTaskManager;
            _sessionManager = sessionManager;
            _config = config;
            _liveTvManager = liveTvManager;
        }

        public void Run()
        {
            if (_appHost.CanSelfRestart)
            {
                _appHost.HasPendingRestartChanged += _appHost_HasPendingRestartChanged;
            }
        }

        void _appHost_HasPendingRestartChanged(object sender, EventArgs e)
        {
            DisposeTimer();

            if (_appHost.HasPendingRestart)
            {
                _timer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            }
        }

        private async void TimerCallback(object state)
        {
            if (_config.Configuration.EnableAutomaticRestart)
            {
                var isIdle = await IsIdle().ConfigureAwait(false);

                if (isIdle)
                {
                    DisposeTimer();

                    try
                    {
                        _appHost.Restart();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error restarting server", ex);
                    }
                }
            }
        }

        private async Task<bool> IsIdle()
        {
            if (_iTaskManager.ScheduledTasks.Any(i => i.State != TaskState.Idle))
            {
                return false;
            }

            if (_liveTvManager.Services.Count == 1)
            {
                try
                {
                    var timers = await _liveTvManager.GetTimers(new TimerQuery(), CancellationToken.None).ConfigureAwait(false);
                    if (timers.Items.Any(i => i.Status == RecordingStatus.InProgress))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting timers", ex);
                }
            }

            var now = DateTime.UtcNow;

            return !_sessionManager.Sessions.Any(i => (now - i.LastActivityDate).TotalMinutes < 30);
        }

        public void Dispose()
        {
            _appHost.HasPendingRestartChanged -= _appHost_HasPendingRestartChanged;

            DisposeTimer();
        }

        private void DisposeTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
