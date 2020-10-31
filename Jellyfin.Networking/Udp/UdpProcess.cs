using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Networking.Udp
{
    /// <summary>
    /// Provides logging capabilities.
    /// </summary>
    /// <param name="client">Client to whom the failure relates.</param>
    /// <param name="ex">Optional exception details.</param>
    /// <param name="msg">Optional message.</param>
    public delegate void FailureFunction(UdpProcess client, Exception? ex = null, string? msg = null);

    /// <summary>
    /// Client to be used with <seealso cref="UdpHelper"/>.
    /// </summary>
    public class UdpProcess : UdpClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UdpProcess"/> class.
        /// </summary>
        /// <param name="localIpAddress">Local ip address.</param>
        /// <param name="portNumber">local port number.</param>
        /// <param name="processor">Receiving parser function.</param>
        /// <param name="logger">Logger instance to use.</param>
        /// <param name="failure">Failure function.</param>
        public UdpProcess(IPAddress localIpAddress, int portNumber, UdpProcessor? processor = null, ILogger? logger = null, FailureFunction? failure = null)
            : base(localIpAddress?.AddressFamily ?? throw new NullReferenceException(nameof(localIpAddress)))
        {
            LocalEndPoint = new IPEndPoint(
                UdpHelper.EnableMultiSocketBinding ? localIpAddress :
                    localIpAddress.AddressFamily == AddressFamily.InterNetwork ?
                        IPAddress.Any : IPAddress.IPv6Any,
                portNumber);

            Processor = processor;
            Logger = logger;
            OnFailure = failure;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpProcess"/> class.
        /// </summary>
        /// <param name="localIpAddress">IP Address to assign.</param>
        private UdpProcess(IPAddress localIpAddress)
        {
            LocalEndPoint = new IPEndPoint(localIpAddress, 0);
        }

        /// <summary>
        /// Gets or sets user defined information.
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets the local endpoint of this client.
        /// </summary>
        public IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets or sets the optional processing function. Called when data is recieved on this port.
        /// </summary>
        public UdpProcessor? Processor { get; set; }

        /// <summary>
        /// Gets or sets the optional logger instance.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Gets or sets the optional failure function. Called when an error occurs in the receiver.
        /// </summary>
        public FailureFunction? OnFailure { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether traffic through this port should be logged.
        /// </summary>
        public bool Tracing { get; set; }

        /// <summary>
        /// Gets or sets the trace function.
        /// </summary>
        public IPAddress? TracingFilter { get; set; }

        /// <summary>
        /// Creates an isolated udpProcess instance designed not to be used for UDP work.
        /// </summary>
        /// <param name="localIpAddress">IP Address to assign.</param>
        /// <returns>Uninitialised UdpProcess.</returns>
        public static UdpProcess CreateIsolated(IPAddress localIpAddress)
        {
            return new UdpProcess(localIpAddress);
        }

        /// <summary>
        /// Binds to the local endpoint in LocalEndPoint.
        /// </summary>
        public void Bind()
        {
            Client.Bind(LocalEndPoint);
        }

        /// <summary>
        /// Logs traffic flowing through this udp port.
        /// </summary>
        /// <param name="msg">Message to log.</param>
        /// <param name="localIpAddress">Local ip address.</param>
        /// <param name="remote">Remote ip address.</param>
        /// <param name="parameters">Optional parameters to include in the message.</param>
        public void Track(string msg, IPEndPoint localIpAddress, IPEndPoint remote, params object[] parameters)
        {
            bool log = TracingFilter == null ||
                       TracingFilter.Equals(localIpAddress) ||
                       ((remote != null) && TracingFilter.Equals(remote.Address)) ||
                       (localIpAddress != null && (localIpAddress.Equals(IPAddress.Any) || localIpAddress.Equals(IPAddress.IPv6Any)));

            if (log)
            {
                switch (parameters.Length)
                {
                    case 0:
                        Logger.LogDebug(msg, localIpAddress, remote);
                        break;
                    case 1:
                        Logger.LogDebug(msg, localIpAddress, remote, parameters[0]);
                        break;
                    case 2:
                        Logger.LogDebug(msg, localIpAddress, remote, parameters[0], parameters[1]);
                        break;
                    case 3:
                        Logger.LogDebug(msg, localIpAddress, remote, parameters[0], parameters[1], parameters[2]);
                        break;
                    case 4:
                        Logger.LogDebug(msg, localIpAddress, remote, parameters[0], parameters[1], parameters[2], parameters[3]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(parameters), "Tracing only supports up to 4 parameters");
                }
            }
        }
    }
}
