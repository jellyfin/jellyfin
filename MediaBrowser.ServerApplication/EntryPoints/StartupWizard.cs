using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace MediaBrowser.ServerApplication.EntryPoints
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
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupWizard" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="userManager">The user manager.</param>
        public StartupWizard(IServerApplicationHost appHost, IUserManager userManager, IServerConfigurationManager configurationManager)
        {
            _appHost = appHost;
            _userManager = userManager;
            _configurationManager = configurationManager;
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
            var user = _userManager.Users.FirstOrDefault(u => u.Configuration.IsAdministrator);

            try
            {
                App.OpenDashboardPage("wizardstart.html", user, _configurationManager, _appHost);
            }
            catch (Win32Exception ex)
            {
                _logger.ErrorException("Error launching startup wizard", ex);

                MessageBox.Show("There was an error launching the Media Browser startup wizard. Please ensure a web browser is installed on the machine and is configured as the default browser.", "Media Browser");
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
