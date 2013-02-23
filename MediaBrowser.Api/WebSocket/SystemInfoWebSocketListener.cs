using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System.Threading.Tasks;

namespace MediaBrowser.Api.WebSocket
{
    /// <summary>
    /// Class SystemInfoWebSocketListener
    /// </summary>
    public class SystemInfoWebSocketListener : BasePeriodicWebSocketListener<SystemInfo, object>
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
        private readonly IKernel _kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInfoWebSocketListener" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        public SystemInfoWebSocketListener(Kernel kernel, ILogger logger)
            : base(logger)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<SystemInfo> GetDataToSend(object state)
        {
            return Task.FromResult(_kernel.GetSystemInfo());
        }
    }
}
