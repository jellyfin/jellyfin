using System.Threading.Tasks;
using Emby.Server.Implementations.Browser;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// Class StartupWizard.
    /// </summary>
    public sealed class StartupWizard : IServerEntryPoint
    {
        /// <summary>
        /// The app host.
        /// </summary>
        private readonly IServerApplicationHost _appHost;
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupWizard"/> class.
        /// </summary>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        public StartupWizard(IServerApplicationHost appHost, IServerConfigurationManager config)
        {
            _appHost = appHost;
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
