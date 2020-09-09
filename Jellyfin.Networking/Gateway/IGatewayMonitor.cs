using System;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface for GatewayMonitor.
    /// </summary>
    public interface IGatewayMonitor
    {
        /// <summary>
        /// Event that gets called every time a ping to the gateway fails.
        /// </summary>
        public event EventHandler? OnGatewayFailure;

        /// <summary>
        /// Adds a gateway for monitoring.
        /// </summary>
        /// <param name="gwAddress">IP Address to monitor.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task AddGateway(IPAddress gwAddress);

        /// <summary>
        /// Clears all the gateways.
        /// </summary>
        public void ResetGateways();
    }
}
