using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
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
                SystemHelper.ResetStandbyTimer();
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

    internal enum EXECUTION_STATE : uint
    {
        ES_NONE = 0,
        ES_SYSTEM_REQUIRED = 0x00000001,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_USER_PRESENT = 0x00000004,
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000
    }

    public class SystemHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        public static void ResetStandbyTimer()
        {
            EXECUTION_STATE es = SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);
        }
    }
}
