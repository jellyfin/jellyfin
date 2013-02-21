using MediaBrowser.ApiInteraction;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Connectivity;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Playback;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Controller
{
    /// <summary>
    /// This controls application logic as well as server interaction within the UI.
    /// </summary>
    public class UIKernel : BaseKernel<UIApplicationConfiguration, UIApplicationPaths>
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static UIKernel Instance { get; private set; }

        /// <summary>
        /// Gets the API client.
        /// </summary>
        /// <value>The API client.</value>
        public ApiClient ApiClient { get; private set; }

        /// <summary>
        /// Gets the playback manager.
        /// </summary>
        /// <value>The playback manager.</value>
        public PlaybackManager PlaybackManager { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UIKernel" /> class.
        /// </summary>
        public UIKernel(IIsoManager isoManager)
            : base(isoManager)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the media players.
        /// </summary>
        /// <value>The media players.</value>
        [ImportMany(typeof(BaseMediaPlayer))]
        public IEnumerable<BaseMediaPlayer> MediaPlayers { get; private set; }

        /// <summary>
        /// Gets the list of currently loaded themes
        /// </summary>
        /// <value>The themes.</value>
        [ImportMany(typeof(BaseTheme))]
        public IEnumerable<BaseTheme> Themes { get; private set; }

        /// <summary>
        /// Gets the kernel context.
        /// </summary>
        /// <value>The kernel context.</value>
        public override KernelContext KernelContext
        {
            get { return KernelContext.Ui; }
        }

        /// <summary>
        /// Gets the UDP server port number.
        /// </summary>
        /// <value>The UDP server port number.</value>
        public override int UdpServerPortNumber
        {
            get { return 7360; }
        }

        /// <summary>
        /// Give the UI a different url prefix so that they can share the same port, in case they are installed on the same machine.
        /// </summary>
        /// <value>The HTTP server URL prefix.</value>
        public override string HttpServerUrlPrefix
        {
            get
            {
                return "http://+:" + Configuration.HttpServerPortNumber + "/mediabrowserui/";
            }
        }

        /// <summary>
        /// Reload api client and update plugins after loading configuration
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task OnConfigurationLoaded()
        {
            ReloadApiClient();

            try
            {
                await new PluginUpdater(Logger).UpdatePlugins().ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error updating plugins from the server", ex);
            }
        }

        /// <summary>
        /// Disposes the current ApiClient and creates a new one
        /// </summary>
        private void ReloadApiClient()
        {
            DisposeApiClient();

            var logger = LogManager.GetLogger("ApiClient");

            ApiClient = new ApiClient(logger, new AsyncHttpClient(new WebRequestHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate,
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.Revalidate)
            }))
            {
                ServerHostName = Configuration.ServerHostName,
                ServerApiPort = Configuration.ServerApiPort,
                ClientType = ClientType.Pc,
                DeviceName = Environment.MachineName,
                SerializationFormat = SerializationFormats.Json
            };
        }

        /// <summary>
        /// Reloads the internal.
        /// </summary>
        /// <returns>Task.</returns>
        protected override Task ReloadInternal()
        {
            PlaybackManager = new PlaybackManager(this);

            return base.ReloadInternal();
        }

        /// <summary>
        /// Called when [composable parts loaded].
        /// </summary>
        /// <returns>Task.</returns>
        protected override async Task OnComposablePartsLoaded()
        {
            await base.OnComposablePartsLoaded().ConfigureAwait(false);

            // Once plugins have loaded give the api a reference to our protobuf serializer
            DataSerializer.DynamicSerializer = ProtobufSerializer.TypeModel;
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeApiClient();
            }

            base.Dispose(dispose);
        }
    }
}
