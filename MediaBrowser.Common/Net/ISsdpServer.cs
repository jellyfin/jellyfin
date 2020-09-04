using System;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Networking;
using SsdpMessage = System.Collections.Generic.Dictionary<string, string>;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface for SsdpServer.
    /// </summary>
    public interface ISsdpServer
    {
        /// <summary>
        /// Gets a value indicating whether uPNP port forwarding is active.
        /// </summary>
        bool IsUPnPActive { get; }

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        bool Tracing { get; }

         /// <summary>
        /// Gets or sets a value indicating the tracing filter to be applied.
        /// </summary>
        IPAddress TracingFilter { get; set; }

        /// <summary>
        /// Gets the number of times each udp packet should be sent.
        /// </summary>
        int UdpSendCount { get; }

        /// <summary>
        /// Adds an event.
        /// </summary>
        /// <param name="action">The string to event on.</param>
        /// <param name="handler">The handler to call.</param>
        void AddEvent(string action, EventHandler<SsdpEventArgs> handler);

        /// <summary>
        /// Removes an event.
        /// </summary>
        /// <param name="action">The event to remove.</param>
        /// <param name="handler">The handler to remove.</param>
        void DeleteEvent(string action, EventHandler<SsdpEventArgs> handler);

        /// <summary>
        /// Multicasts an SSDP package, across all relevant interfaces types.
        /// </summary>
        /// <param name="values">Values that make up the message.</param>
        /// <param name="classification">Classification of message to send.</param>
        /// <param name="advertising">If provided, contain the address embedded in the message that is being advertised.</param>
        /// <param name="sendCount">Optional value indicating the number of times to transmit the message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendMulticastSSDP(SsdpMessage values, string classification, IPAddress advertising = null, int? sendCount = null);

        /// <summary>
        /// Unicasts an SSDP message.
        /// </summary>
        /// <param name="values">Values that make up the message.</param>
        /// <param name="classification">Classification of message to send.</param>
        /// <param name="localIP">Local endpoint to use.</param>
        /// <param name="endPoint">Remote endpoint to transmit to.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendSSDP(SsdpMessage values, string classification, IPAddress localIP, IPEndPoint endPoint);
    }
}
