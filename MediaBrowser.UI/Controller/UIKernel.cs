using MediaBrowser.ApiInteraction;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Progress;
using MediaBrowser.UI.Configuration;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Controller
{
    /// <summary>
    /// This controls application logic as well as server interaction within the UI.
    /// </summary>
    public class UIKernel : BaseKernel<UIApplicationConfiguration, UIApplicationPaths>
    {
        public static UIKernel Instance { get; private set; }

        public ApiClient ApiClient { get; private set; }
        public DtoUser CurrentUser { get; set; }
        public ServerConfiguration ServerConfiguration { get; set; }

        public UIKernel()
            : base()
        {
            Instance = this;
        }

        public override KernelContext KernelContext
        {
            get { return KernelContext.Ui; }
        }

        /// <summary>
        /// Give the UI a different url prefix so that they can share the same port, in case they are installed on the same machine.
        /// </summary>
        protected override string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + Configuration.HttpServerPortNumber + "/mediabrowser/ui/";
            }
        }

        /// <summary>
        /// Performs initializations that can be reloaded at anytime
        /// </summary>
        protected override async Task ReloadInternal(IProgress<TaskProgress> progress)
        {
            ReloadApiClient();

            await new PluginUpdater().UpdatePlugins().ConfigureAwait(false);

            await base.ReloadInternal(progress).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates and installs new plugin assemblies and configurations from the server
        /// </summary>
        protected async Task<PluginUpdateResult> UpdatePlugins()
        {
            return await new PluginUpdater().UpdatePlugins().ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the current ApiClient and creates a new one
        /// </summary>
        private void ReloadApiClient()
        {
            DisposeApiClient();

            ApiClient = new ApiClient
            {
                ServerHostName = Configuration.ServerHostName,
                ServerApiPort = Configuration.ServerApiPort
            };
        }

        /// <summary>
        /// Disposes the current ApiClient
        /// </summary>
        private void DisposeApiClient()
        {
            if (ApiClient != null)
            {
                ApiClient.Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            DisposeApiClient();
        }
    }
}
