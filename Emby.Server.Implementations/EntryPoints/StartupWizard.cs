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
        /// <param name="appConfig">The configuration.</param>
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
            if (!_appHost.CanLaunchWebBrowser)
            {
                return Task.CompletedTask;
            }

            if (!_appConfig.HostWebClient())
            {
                BrowserLauncher.OpenSwaggerPage(_appHost);
            }
            else if (!_config.Configuration.IsStartupWizardCompleted)
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
