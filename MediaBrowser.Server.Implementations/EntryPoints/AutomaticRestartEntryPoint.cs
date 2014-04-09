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

namespace MediaBrowser.Server.Implementations.EntryPoints
{
    public class AutomaticRestartEntryPoint : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly ITaskManager _iTaskManager;
        private readonly ISessionManager _sessionManager;
        private readonly IServerConfigurationManager _config;

        private Timer _timer;

        public AutomaticRestartEntryPoint(IServerApplicationHost appHost, ILogger logger, ITaskManager iTaskManager, ISessionManager sessionManager, IServerConfigurationManager config)
        {
            _appHost = appHost;
            _logger = logger;
            _iTaskManager = iTaskManager;
            _sessionManager = sessionManager;
            _config = config;
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
                _timer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
        }

        private void TimerCallback(object state)
        {
            if (_config.Configuration.EnableAutomaticRestart && IsIdle())
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

        private bool IsIdle()
        {
            if (_iTaskManager.ScheduledTasks.Any(i => i.State != TaskState.Idle))
            {
                return false;
            }

            var now = DateTime.UtcNow;

            return !_sessionManager.Sessions.Any(i => !string.IsNullOrEmpty(i.NowViewingItemName) || (now - i.LastActivityDate).TotalMinutes < 30);
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
