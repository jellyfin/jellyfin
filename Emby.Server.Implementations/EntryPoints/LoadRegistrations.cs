using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class LoadRegistrations
    /// </summary>
    public class LoadRegistrations : IServerEntryPoint
    {
        /// <summary>
        /// The _security manager
        /// </summary>
        private readonly ISecurityManager _securityManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private ITimer _timer;
        private readonly ITimerFactory _timerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadRegistrations" /> class.
        /// </summary>
        /// <param name="securityManager">The security manager.</param>
        /// <param name="logManager">The log manager.</param>
        public LoadRegistrations(ISecurityManager securityManager, ILogManager logManager, ITimerFactory timerFactory)
        {
            _securityManager = securityManager;
            _timerFactory = timerFactory;

            _logger = logManager.GetLogger("Registration Loader");
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            _timer = _timerFactory.Create(s => LoadAllRegistrations(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromHours(12));
        }

        private async Task LoadAllRegistrations()
        {
            try
            {
                await _securityManager.LoadAllRegistrationInfo().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error loading registration info", ex);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
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
