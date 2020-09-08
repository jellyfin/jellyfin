using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Networking.Manager;
using Jellyfin.Networking.Structures;
using Jellyfin.Networking.Udp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;
using Mono.Nat;
// using Mono.Nat;

namespace Jellyfin.Networking.Ssdp
{
    using SsdpMessage = System.Collections.Generic.Dictionary<string, string>;

    /// <summary>
    /// Provides the platform independent logic for publishing device existence and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    ///
    /// Lazy implementation. Socks will only be created at first use.
    /// </summary>
    public class SsdpServer : UdpServer, ISsdpServer
    {
        private static readonly string _hostName = $"{MediaBrowser.Common.System.OperatingSystem.Name}/{Environment.OSVersion.VersionString} UPnP/1.0 RSSDP/1.0";
        private static SsdpServer? _instance;
        private readonly object _synchroniser;
        private readonly IServerApplicationHost _appHost;
        private readonly Hashtable _listeners;
        private readonly Hashtable _senders;
        private readonly Dictionary<string, List<EventHandler<SsdpEventArgs>>> _events;
        private NetCollection _interfaces;
        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        /// <param name="networkManager">The NetManager<see cref="INetworkManager"/>.</param>
        /// <param name="configurationManager">The system configuration.</param>
        /// <param name="logger">The logger instance.<see cref="ILogger"/>.</param>
        /// <param name="appHost">The application host.</param>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable : Declared in UpdateSettings.
        public SsdpServer(
            INetworkManager networkManager,
            IConfigurationManager configurationManager,
            ILogger logger,
            IServerApplicationHost appHost)
            : base(networkManager, configurationManager, logger)
        {
            _appHost = appHost;
            _synchroniser = new object();
            _listeners = new Hashtable();
            _senders = new Hashtable();
            _events = new Dictionary<string, List<EventHandler<SsdpEventArgs>>>();
            UpdateArguments();
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        /// <summary>
        /// Gets a value indicating whether uPNP port forwarding is active.
        /// </summary>
        public bool IsUPnPActive { get; private set; }

        /// <summary>
        /// Gets the number of times each udp packet should be sent.
        /// </summary>
        public int UdpSendCount { get => Configuration.UDPSendCount; }

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        public bool Tracing { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating the tracing filter to be applied.
        /// </summary>
        public IPAddress? TracingFilter { get; set; }

        /// <summary>
        /// Gets or creates the singleton instance.
        /// </summary>
        /// <param name="networkManager">Network manager instance.</param>
        /// <param name="configurationManager">Configuration manager instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="appHost">Application instance.</param>
        /// <returns>The SsdpServer singleton instance.</returns>
        public static ISsdpServer GetOrCreateInstance(
            INetworkManager networkManager,
            IConfigurationManager configurationManager,
            ILogger logger,
            IServerApplicationHost appHost)
        {
            if (_instance == null)
            {
                _instance = new SsdpServer(networkManager, configurationManager, logger, appHost);
            }

            return _instance;
        }

        /// <summary>
        /// Formats a SsdpMessage for output.
        /// </summary>
        /// <param name="m">Ssdp message to output.</param>
        /// <returns>Formatted message.</returns>
        public static string DebugOutput(SsdpMessage m)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var l in m ?? throw new ArgumentNullException(nameof(m)))
            {
                sb.Append(l.Key);
                sb.Append(": ");
                sb.AppendLine(l.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Adds an event.
        /// </summary>
        /// <param name="action">The string to event on.</param>
        /// <param name="handler">The handler to call.</param>
        public void AddEvent(string action, EventHandler<SsdpEventArgs> handler)
        {
            lock (_synchroniser)
            {
                if (!_events.ContainsKey(action))
                {
                    _events[action] = new List<EventHandler<SsdpEventArgs>>();
                }

                if (_events[action].IndexOf(handler) == -1)
                {
                    _events[action].Add(handler);
                }
            }

            StartServer();
        }

        /// <summary>
        /// Removes an event.
        /// </summary>
        /// <param name="action">The event to remove.</param>
        /// <param name="handler">The handler to remove.</param>
        public void DeleteEvent(string action, EventHandler<SsdpEventArgs> handler)
        {
            lock (_synchroniser)
            {
                if (_events.ContainsKey(action))
                {
                    _events[action].Remove(handler);
                    if (_events[action].Count == 0)
                    {
                        _events.Remove(action);
                    }
                }
            }

            if (_events.Count == 0)
            {
                StopServer();
            }
        }

        /// <summary>
        /// Multicasts an SSDP package, across all relevant interfaces types.
        /// </summary>
        /// <param name="values">Values that make up the message.</param>
        /// <param name="classification">Classification of message to send.</param>
        /// <param name="advertising">If provided, contain the address embedded in the message that is being advertised.</param>
        /// <param name="sendCount">Optional value indicating the number of times to transmit the message.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendMulticastSSDP(SsdpMessage values, string classification, IPAddress? advertising = null, int? sendCount = null)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (advertising != null)
            {
                // Don't advertise an addressfamily which we aren't enabled for.
                if ((advertising.AddressFamily == AddressFamily.InterNetwork && !IsIP4Enabled) ||
                    (advertising.AddressFamily == AddressFamily.InterNetworkV6 && !IsIP6Enabled))
                {
                    return;
                }
            }

            foreach (var ipEntry in _senders.Keys)
            {
                if (ipEntry != null)
                {
                    var addr = (IPAddress)ipEntry;
                    if (((advertising != null) && (addr.AddressFamily != advertising.AddressFamily)) || (addr.AddressFamily == AddressFamily.InterNetworkV6 && addr.ScopeId == 0))
                    {
                        continue;
                    }

                    var mcast = addr.AddressFamily == AddressFamily.InterNetwork ?
                        IPNetAddress.MulticastIPv4 : IPObject.IsIPv6LinkLocal(addr) ?
                            IPNetAddress.MulticastIPv6LinkLocal : IPNetAddress.MulticastIPv6SiteLocal;

                    values["HOST"] = mcast.ToString() + ":1900";

                    var message = BuildMessage(classification, values);

                    var client = _senders[ipEntry];
                    if (client != null)
                    {
                        await SendMulticast((UdpProcess)client, 1900, message, sendCount ?? UdpSendCount).ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.LogError("Unable to find client for {0}", addr);
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Unicasts an SSDP message.
        /// </summary>
        /// <param name="values">Values that make up the message.</param>
        /// <param name="classification">Classification of message to send.</param>
        /// <param name="localIP">Local endpoint to use.</param>
        /// <param name="endPoint">Remote endpoint to transmit to.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendSSDP(SsdpMessage values, string classification, IPAddress localIP, IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            var message = BuildMessage(classification, values);

            await SendMessageAsync(message, localIP, endPoint, true).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void UpdateArguments()
        {
            base.UpdateArguments();

            _interfaces = NetManager.GetInternalBindAddresses();

            if (!string.IsNullOrEmpty(Configuration.SSDPTracingFilter))
            {
                if (IPAddress.TryParse(Configuration.SSDPTracingFilter, out IPAddress result))
                {
                    Logger.LogDebug("Filtering on: {0}", result);
                    TracingFilter = result;
                }
                else
                {
                    Logger.LogDebug("SSDPTracingFilter contains an invalid IP address.");
                }
            }

            IPAddress? ipFilter = null;
            if (IPAddress.TryParse(Configuration.SSDPTracingFilter, out IPAddress filter))
            {
                ipFilter = filter;
            }

            if (Configuration.EnableSSDPTracing != Tracing || ipFilter != TracingFilter)
            {
                UdpProcess client;
                foreach (var i in _listeners.Values)
                {
                    if (i != null)
                    {
                        client = (UdpProcess)i;
                        client.TracingFilter = ipFilter;
                        client.Tracing = Configuration.EnableSSDPTracing;
                    }
                }

                foreach (var i in _senders.Values)
                {
                    if (i != null)
                    {
                        client = (UdpProcess)i;
                        client.TracingFilter = ipFilter;
                        client.Tracing = Configuration.EnableSSDPTracing;
                    }
                }

                Tracing = Configuration.EnableSSDPTracing;
                TracingFilter = ipFilter;
            }

            IsUPnPActive = Configuration.EnableUPnP &&
                           Configuration.EnableRemoteAccess &&
                           (_appHost.ListenWithHttps || (!_appHost.ListenWithHttps && Configuration.UPnPCreateHttpPortMap));

            // if (IsUPnPActive)
            // {
            //     NatUtility.UnknownDeviceFound += UnknownDeviceFound;
            // }
            // else
            // {
            //     NatUtility.UnknownDeviceFound -= UnknownDeviceFound;
            // }
        }

        /// <summary>
        /// Builds an SSDP message.
        /// </summary>
        /// <param name="header">SSDP Header string.</param>
        /// <param name="values">SSDP paramaters.</param>
        /// <returns>Formatted string.</returns>
        private string BuildMessage(string header, Dictionary<string, string> values)
        {
            const string SsdpOpt = "\"http://schemas.upnp.org/upnp/1/0/\"; ns=";

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            string nc = NetworkManager.NetworkChangeCount.ToString("d2", CultureInfo.InvariantCulture);
            values["SERVER"] = _hostName;
            // Optional headers.
            values["OPT"] = SsdpOpt + nc;
            values["CONFIGID.UPNP.ORG"] = "1";
            values["BOOTID.UPNP.ORG"] = values[nc + "-NLS"] = NetworkManager.NetworkLocationSignature;

            var builder = new StringBuilder();

            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\r\n", header);

            foreach (var pair in values)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", pair.Key, pair.Value);
            }

            builder.Append("\r\n\r\n");

            return builder.ToString();
        }

        /// <summary>
        /// Sends a message to the SSDP multicast address and port.
        /// </summary>
        /// <param name="message">The mesage to send.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <param name="endPoint">The destination endpoint.</param>
        /// <param name="restrict">True if the comms should be restricted to the LAN.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendMessageAsync(string message, IPAddress localIPAddress, IPEndPoint endPoint, bool restrict = false)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (localIPAddress == null)
            {
                throw new ArgumentNullException(nameof(localIPAddress));
            }

            if (restrict)
            {
                if (!NetManager.IsInLocalNetwork(endPoint.Address))
                {
                    Logger.LogDebug("FILTERED: Sending to non-LAN address: {0}.", endPoint.Address);
                    return;
                }

                // Are we sending to Mono.Nat?
                if (IsUPnPActive && endPoint.Port == 1900 && NetManager.IsGatewayInterface(endPoint.Address))
                {
                    return;
                }
            }

            var client = _senders[localIPAddress];
            if (client != null)
            {
                await SendUnicast((UdpProcess)client, message, endPoint).ConfigureAwait(false);
            }
            else
            {
                Logger.LogError("Unable to find socket for {0}", localIPAddress);
            }
        }

        private void OnFailure(UdpProcess client, Exception? ex = null, string? msg = null)
        {
            _listeners.Remove(client.LocalEndPoint.Address);
            Logger.LogError(ex, msg);
        }

        /// <summary>
        /// Sends a packet via unicast.
        /// </summary>
        private SsdpMessage ParseMessage(string data)
        {
            var result = new SsdpMessage(StringComparer.OrdinalIgnoreCase);
            int i;
            var lines = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                i = line.IndexOf(':', StringComparison.OrdinalIgnoreCase);
                if (i != -1)
                {
                    string propertyName = line.Substring(0, i).ToUpper(CultureInfo.InvariantCulture);
                    if (!result.ContainsKey(propertyName))
                    {
                        result.Add(propertyName, line.Substring(i + 1).Trim());
                    }
                    else
                    {
                        Logger.LogDebug("{0} appears twice: {1}", propertyName, data);
                    }
                }
                else
                {
                    i = line.IndexOf('*', StringComparison.OrdinalIgnoreCase);
                    if (i != -1)
                    {
                        result.Add("ACTION", line.Substring(0, i - 1).Trim());
                    }
                    else if (line.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
                    {
                        result["ACTION"] = line;
                    }
                }
            }

            if (!result.ContainsKey("ACTION"))
            {
                result["ACTION"] = string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Processes a SSDP message.
        /// </summary>
        /// <param name="client">The client from which we received the message.</param>
        /// <param name="data">The data to process.</param>
        /// <param name="receivedFrom">The remote endpoint.</param>
        private Task ProcessMessage(UdpProcess client, string data, IPEndPoint receivedFrom)
        {
            if (!NetManager.IsInLocalNetwork(receivedFrom.Address))
            {
                // Not from the local LAN, so ignore it.
                return Task.CompletedTask;
            }

            // var from = _senders[receivedFrom.Address];
            // if (from != null)
            // {
            //    var fromClient = (UdpProcess)from;
            //    if (fromClient.LocalEndPoint.Equals(receivedFrom))
            //    {
            //        Logger.LogDebug("FILTERING: Message came from us {0}, {1}", fromClient.LocalEndPoint, receivedFrom);
            //        return Task.CompletedTask;
            //    }
            // }

            string action;

            var msg = ParseMessage(data);
            action = msg["ACTION"];

            var localIpAddress = client.LocalEndPoint.Address;

            if (Configuration.EnableSSDPTracing)
            {
                if (TracingFilter == null || TracingFilter.Equals(receivedFrom.Address) || TracingFilter.Equals(localIpAddress))
                {
                    Logger.LogDebug("<- {0} : {1} \r\n{3}", receivedFrom.Address, localIpAddress, DebugOutput(msg));
                }
            }

            List<EventHandler<SsdpEventArgs>> handlers;

            lock (_synchroniser)
            {
                if (!_events.ContainsKey(action))
                {
                    return Task.CompletedTask;
                }

                handlers = _events[action].ToList();
            }

            var args = new SsdpEventArgs(data, msg, receivedFrom, localIpAddress, client == null);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Invoke(this, args);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    Logger.LogError(ex, "Error firing event: {0}", action);
                }
            }

            return Task.CompletedTask;
        }

        private void StartServer()
        {
            if (!_running)
            {
                _running = true;
                NetManager.NetworkChanged += NetworkChanged;

                Logger.LogDebug("EnableMultiSocketBinding : {0}", EnableMultiSocketBinding);

                // if (IsUPnPActive)
                // {
                //    NatUtility.UnknownDeviceFound += UnknownDeviceFound;
                // }

                foreach (IPObject ip in _interfaces)
                {
                    var client = CreateMulticastClient(ip.Address, 1900, ProcessMessage, Logger, OnFailure);
                    if (client != null)
                    {
                        client.TracingFilter = TracingFilter;
                        client.Tracing = Configuration.EnableSSDPTracing;
                        _listeners[ip.Address] = client;
                    }

                    client = CreateUnicastClient(ip.Address, 0, ProcessMessage, Logger, OnFailure);
                    if (client != null)
                    {
                        client.TracingFilter = TracingFilter;
                        client.Tracing = Configuration.EnableSSDPTracing;
                        _senders[ip.Address] = client;
                    }
                }
            }
        }

        private void StopServer()
        {
            if (_running)
            {
                // NatUtility.UnknownDeviceFound -= UnknownDeviceFound;
                NetManager.NetworkChanged -= NetworkChanged;
                _listeners.Clear();
                _senders.Clear();
                _running = false;
            }
        }

        /// <summary>
        /// Triggered on a network change.
        /// </summary>
        /// <param name="sender">NetManager object.</param>
        /// <param name="args">Event arguments.</param>
        private void NetworkChanged(object? sender, System.EventArgs args)
        {
            if (_running && !Disposed)
            {
                StopServer();
                StartServer();
            }
        }

        /// <summary>
        /// Enables the SSDP injection of devices found by Mono.Nat.
        /// </summary>
        /// <param name="sender">Mono.Nat instance.</param>
        /// <param name="e">Information Mono received, but doesn't use.</param>
        private void UnknownDeviceFound(object sender, DeviceEventUnknownArgs e)
        {
            if (!_running || Disposed)
            {
                return;
            }

            IPEndPoint ep = (IPEndPoint)e.EndPoint;
            IPAddress remote = ep.Address;

            // Only process the IP address family that we are configured for.
            if (!IsIP4Enabled && ep.AddressFamily == AddressFamily.InterNetwork)
            {
                return;
            }

            if (!IsIP6Enabled && ep.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return;
            }

            if (NetManager.IsExcluded(remote))
            {
                return;
            }

            if (!NetManager.IsInLocalNetwork(remote) && NetManager.IsValidInterfaceAddress(remote))
            {
                Logger.LogDebug("FILTERED: Sending to non-LAN address: {0}.", remote);
                return;
            }

            if (_senders[ep.Address] != null)
            {
                Logger.LogDebug("FILTERED: Sending to Self: {0} -> {0}/{1}. uPnP?", e.Address, remote, ep.Port);
                return;
            }

            // _logger.LogDebug("Mono.NAT passing information to our SSDP processor.");
            using var dummyUdp = UdpProcess.CreateIsolated(e.Address);
            ProcessMessage(dummyUdp, e.Data, ep);
        }
    }
}
