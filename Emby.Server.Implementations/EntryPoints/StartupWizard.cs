using System.Threading.Tasks;
using Emby.Server.Implementations.Browser;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Configuration;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class StartupWizard.
    /// </summary>
    public sealed class StartupWizard : IServerEntryPoint
    {
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _appConfig;
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupWizard"/> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        public StartupWizard(IServerApplicationHost appHost, IConfiguration appConfig, IServerConfigurationManager config)
        {
            _appHost = appHost;
            _appConfig = appConfig;
            _config = config;
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            Run();
            return Task.CompletedTask;
        }

        private void Run()
        {
            if (!_appHost.CanLaunchWebBrowser)
            {
                return;
            }

            // Always launch the startup wizard if possible when it has not been completed
            if (!_config.Configuration.IsStartupWizardCompleted && _appConfig.HostWebClient())
            {
                BrowserLauncher.OpenWebApp(_appHost);
                return;
            }

            // Do nothing if the web app is configured to not run automatically
            var options = ((ApplicationHost)_appHost).StartupOptions;
            if (!_config.Configuration.AutoRunWebApp || options.NoAutoRunWebApp)
            {
                return;
            }

            // Launch the swagger page if the web client is not hosted, otherwise open the web client
            if (_appConfig.HostWebClient())
            {
                BrowserLauncher.OpenWebApp(_appHost);
            }
            else
            {
                BrowserLauncher.OpenSwaggerPage(_appHost);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
