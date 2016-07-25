using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System.Threading.Tasks;

namespace MediaBrowser.Api.System
{
    /// <summary>
    /// Class SystemInfoWebSocketListener
    /// </summary>
    public class SystemInfoWebSocketListener : BasePeriodicWebSocketListener<SystemInfo, WebSocketListenerState>
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        protected override string Name
        {
            get { return "SystemInfo"; }
        }

        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInfoWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The app host.</param>
        public SystemInfoWebSocketListener(ILogger logger, IServerApplicationHost appHost)
            : base(logger)
        {
            _appHost = appHost;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<SystemInfo> GetDataToSend(WebSocketListenerState state)
        {
            return _appHost.GetSystemInfo();
        }
    }
}
