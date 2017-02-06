using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Interface for a component that manages network communication (sending and receiving HTTPU messages) for the SSDP protocol.
    /// </summary>
    public interface ISsdpCommunicationsServer : IDisposable
    {

        #region Events

        /// <summary>
        /// Raised when a HTTPU request message is received by a socket (unicast or multicast).
        /// </summary>
        event EventHandler<RequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// Raised when an HTTPU response message is received by a socket (unicast or multicast).
        /// </summary>
        event EventHandler<ResponseReceivedEventArgs> ResponseReceived;

        #endregion

        #region Methods

        /// <summary>
        /// Causes the server to begin listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        void BeginListeningForBroadcasts();

        /// <summary>
        /// Causes the server to stop listening for multicast messages, being SSDP search requests and notifications.
        /// </summary>
        void StopListeningForBroadcasts();

        /// <summary>
        /// Stops listening for search responses on the local, unicast socket.
        /// </summary>
        void StopListeningForResponses();

        /// <summary>
        /// Sends a message to a particular address (uni or multicast) and port.
        /// </summary>
        Task SendMessage(byte[] messageData, IpEndPointInfo destination, IpAddressInfo fromLocalIpAddress, CancellationToken cancellationToken);

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        Task SendMulticastMessage(string message, CancellationToken cancellationToken);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a boolean value indicating whether or not this instance is shared amongst multiple <see cref="SsdpDeviceLocatorBase"/> and/or <see cref="ISsdpDevicePublisher"/> instances.
        /// </summary>
        /// <remarks>
        /// <para>If true, disposing an instance of a <see cref="SsdpDeviceLocatorBase"/>or a <see cref="ISsdpDevicePublisher"/> will not dispose this comms server instance. The calling code is responsible for managing the lifetime of the server.</para>
        /// </remarks>
        bool IsShared { get; set; }

        #endregion

    }
}