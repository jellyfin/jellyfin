using System;
using System.Net;
using System.Net.NetworkInformation;

namespace Emby.Server.Implementations.Networking
{
    /// <summary>
    /// EventArgs class.
    /// </summary>
    public class GatewayEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GatewayEventArgs"/> class.
        /// </summary>
        /// <param name="gateway">Gateway address.</param>
        /// <param name="status">Status of the ping.</param>
        public GatewayEventArgs(IPAddress gateway, IPStatus status)
        {
            Status = status;
            Gateway = gateway;
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public IPStatus Status { get; }

        /// <summary>
        /// Gets the gateway.
        /// </summary>
        public IPAddress Gateway { get; }
    }
}
