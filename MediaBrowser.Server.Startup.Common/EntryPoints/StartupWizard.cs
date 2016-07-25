using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common.Browser;

namespace MediaBrowser.Server.Startup.Common.EntryPoints
{
    /// <summary>
    /// Class StartupWizard
    /// </summary>
    public class StartupWizard : IServerEntryPoint
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;
        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupWizard" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="logger">The logger.</param>
        public StartupWizard(IServerApplicationHost appHost, ILogger logger)
        {
            _appHost = appHost;
            _logger = logger;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            if (_appHost.IsFirstRun)
            {
                LaunchStartupWizard();
            }
        }

        /// <summary>
        /// Launches the startup wizard.
        /// </summary>
        private void LaunchStartupWizard()
        {
            BrowserLauncher.OpenDashboardPage("wizardstart.html", _appHost);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}