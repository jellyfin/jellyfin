using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.ServerApplication.EntryPoints
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

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadRegistrations" /> class.
        /// </summary>
        /// <param name="securityManager">The security manager.</param>
        /// <param name="logManager">The log manager.</param>
        public LoadRegistrations(ISecurityManager securityManager, ILogManager logManager)
        {
            _securityManager = securityManager;

            _logger = logManager.GetLogger("Registration Loader");
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public async void Run()
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
        }
    }
}
