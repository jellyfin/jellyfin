using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

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
        public SystemInfoWebSocketListener(ILogger logger, IServerApplicationHost appHost, ITimerFactory timerFactory)
            : base(logger, timerFactory)
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
