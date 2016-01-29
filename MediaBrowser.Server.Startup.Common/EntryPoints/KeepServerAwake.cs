using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using MediaBrowser.Common.Threading;

namespace MediaBrowser.Server.Startup.Common.EntryPoints
{
    public class KeepServerAwake : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private PeriodicTimer _timer;
        private readonly IServerApplicationHost _appHost;

        public KeepServerAwake(ISessionManager sessionManager, ILogger logger, IServerApplicationHost appHost)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            _appHost = appHost;
        }

        public void Run()
        {
            _timer = new PeriodicTimer(obj =>
            {
                var now = DateTime.UtcNow;
                if (_sessionManager.Sessions.Any(i => (now - i.LastActivityDate).TotalMinutes < 15))
                {
                    KeepAlive();
                }

            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void KeepAlive()
        {
            var nativeApp = ((ApplicationHost)_appHost).NativeApp;

            try
            {
                nativeApp.PreventSystemStandby();
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
