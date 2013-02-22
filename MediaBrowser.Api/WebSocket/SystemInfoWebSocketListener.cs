using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MediaBrowser.Api.WebSocket
{
    /// <summary>
    /// Class SystemInfoWebSocketListener
    /// </summary>
    [Export(typeof(IWebSocketListener))]
    public class SystemInfoWebSocketListener : BasePeriodicWebSocketListener<IKernel, SystemInfo, object>
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
        /// Initializes a new instance of the <see cref="SystemInfoWebSocketListener" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        [ImportingConstructor]
        public SystemInfoWebSocketListener([Import("logger")] ILogger logger)
            : base(logger)
        {
            
        }

        /// <summary>
        /// Gets the data to send.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{SystemInfo}.</returns>
        protected override Task<SystemInfo> GetDataToSend(object state)
        {
            return Task.FromResult(Kernel.GetSystemInfo());
        }
    }
}
