using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.EntryPoints
{
    public class KeepServerAwake : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private ITimer _timer;
        private readonly IServerApplicationHost _appHost;
        private readonly ITimerFactory _timerFactory;
        private readonly IPowerManagement _powerManagement;

        public KeepServerAwake(ISessionManager sessionManager, ILogger logger, IServerApplicationHost appHost, ITimerFactory timerFactory, IPowerManagement powerManagement)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            _appHost = appHost;
            _timerFactory = timerFactory;
            _powerManagement = powerManagement;
        }

        public void Run()
        {
            _timer = _timerFactory.Create(OnTimerCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void OnTimerCallback(object state)
        {
            var now = DateTime.UtcNow;

            try
            {
                if (_sessionManager.Sessions.Any(i => (now - i.LastActivityDate).TotalMinutes < 15))
                {
                    _powerManagement.PreventSystemStandby();
                }
                else
                {
                    _powerManagement.AllowSystemStandby();
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error resetting system standby timer", ex);
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
