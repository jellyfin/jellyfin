using System.Threading.Tasks;
using Emby.Server.Implementations.Browser;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class StartupWizard.
    /// </summary>
    public class StartupWizard : IServerEntryPoint
    {
        /// <summary>
        /// The app host.
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The user manager.
        /// </summary>
        private readonly ILogger _logger;

        private IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupWizard"/> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="config">The configuration manager.</param>
        public StartupWizard(IServerApplicationHost appHost, ILogger logger, IServerConfigurationManager config)
        {
            _appHost = appHost;
            _logger = logger;
            _config = config;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            if (!_appHost.CanLaunchWebBrowser)
            {
                return Task.CompletedTask;
            }

            if (!_config.Configuration.IsStartupWizardCompleted)
            {
                BrowserLauncher.OpenWebApp(_appHost);
            }
            else if (_config.Configuration.AutoRunWebApp)
            {
                var options = ((ApplicationHost)_appHost).StartupOptions;

                if (!options.NoAutoRunWebApp)
                {
                    BrowserLauncher.OpenWebApp(_appHost);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
