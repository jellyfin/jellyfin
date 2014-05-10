using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.ServerApplication.Native;
using System;
using System.Linq;
using System.Threading;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    public class KeepServerAwake : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private Timer _timer;

        public KeepServerAwake(ISessionManager sessionManager, ILogger logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public void Run()
        {
            _timer = new Timer(obj =>
            {
                var now = DateTime.UtcNow;
                if (_sessionManager.Sessions.Any(i => (now - i.LastActivityDate).TotalMinutes < 5))
                {
                    KeepAlive();
                }

            }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void KeepAlive()
        {
            try
            {
                NativeApp.PreventSystemStandby();
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
