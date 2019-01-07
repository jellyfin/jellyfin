using Emby.Server.Implementations.Browser;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;
using MediaBrowser.Controller.Configuration;

namespace Emby.Server.Implementations.EntryPoints
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

        private IServerConfigurationManager _config;

        public StartupWizard(IServerApplicationHost appHost, ILogger logger, IServerConfigurationManager config)
        {
            _appHost = appHost;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            if (!_appHost.CanLaunchWebBrowser)
            {
                return;
            }

            if (!_config.Configuration.IsStartupWizardCompleted)
            {
                BrowserLauncher.OpenWebApp(_appHost);
            }
            else if (_config.Configuration.AutoRunWebApp)
            {
                var options = ((ApplicationHost)_appHost).StartupOptions;

                if (!options.ContainsOption("-noautorunwebapp"))
                {
                    BrowserLauncher.OpenWebApp(_appHost);
                }
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
