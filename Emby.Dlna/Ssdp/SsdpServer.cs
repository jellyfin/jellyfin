#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Udp;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.Ssdp
{
    /// <summary>
    /// Publishing SSDP devices and responding to search requests.
    ///
    /// Is designed to work in conjunction with ExternalPortForwarding.
    /// </summary>
    public class SsdpServer : ISsdpServer
    {
        private static string _networkLocationSignature = Guid.NewGuid().ToString();
        private static int _networkChangeCount = 1;
        private static SsdpServer? _instance;
        private readonly object _synchroniser;
        private readonly ILogger _logger;
        private readonly Hashtable _listeners;
        private readonly Hashtable _senders;
        private readonly INetworkManager _networkManager;
        private readonly Dictionary<string, List<EventHandler<SsdpEventArgs>>> _events;
        private readonly object _eventFireLock;
        private Collection<IPObject> _interfaces;
        private bool _running;
        private bool _eventfire;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpServer"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.<see cref="ILogger"/>.</param>
        /// <param name="interfaces">Interfaces to use for the server.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance to use.</param>
        private SsdpServer(ILogger logger, Collection<IPObject> interfaces, INetworkManager networkManager)
        {
            _logger = logger;
            _eventFireLock = new object();
            _synchroniser = new object();
            _listeners = new Hashtable();
            _senders = new Hashtable();
            _events = new Dictionary<string, List<EventHandler<SsdpEventArgs>>>();
            UdpSendCount = 2;
            _interfaces = interfaces;
            _networkManager = networkManager;
        }

        /// <summary>
        /// Gets or sets the Host name to be used in SSDP packets.
        /// </summary>
        public static string HostName { get; set; } = $"OS/{Environment.OSVersion.VersionString} UPnP/1.0 RSSDP/1.0";

        /// <summary>
        /// Gets or sets a value indicating the number of times each udp packet should be sent.
        /// </summary>
        public int UdpSendCount { get; set; }

        /// <summary>
        /// Gets a value indicating whether detailed DNLA debug logging is active.
        /// </summary>
        public bool Tracing { get; internal set; }

        /// <summary>
        /// Gets a value indicating the tracing filter to be applied.
        /// </summary>
        public IPAddress? TracingFilter { get; internal set; }

        /// <summary>
        /// Gets or creates the singleton instance that is used by all objects.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="interfaces">Interfaces to use for the server.</param>
        /// <param name="networkManager">The <see cref="INetworkManager"/> instance to use.</param>
        /// <returns>The SsdpServer singleton instance.</returns>
        public static ISsdpServer GetOrCreateInstance(
            ILogger logger,
            Collection<IPObject> interfaces,
            INetworkManager networkManager)
        {
            // As this class is used in multiple areas, we only want to create it once.
            if (_instance == null)
            {
                _instance = new SsdpServer(logger, interfaces, networkManager);
            }

            return _instance;
        }

        /// <summary>
        /// Formats a SsdpMessage for output.
        /// </summary>
        /// <param name="m">Ssdp message to output.</param>
        /// <returns>Formatted message.</returns>
        public static string DebugOutput(Dictionary<string, string> m)
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

                // Ensure we only add the handler once.
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
        public async Task SendMulticastSSDP(Dictionary<string, string> values, string classification, IPAddress? advertising = null, int? sendCount = null)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (advertising != null)
            {
                // Don't advertise an address family which we aren't enabled for.
                if ((advertising.AddressFamily == AddressFamily.InterNetwork && !_networkManager.IsIP4Enabled)
                    || (advertising.AddressFamily == AddressFamily.InterNetworkV6 && !_networkManager.IsIP6Enabled))
                {
                    return;
                }
            }

            foreach (var ipEntry in _senders.Keys)
            {
                if (ipEntry != null)
                {
                    var addr = (IPAddress)ipEntry;
                    if (((advertising != null)
                        && (addr.AddressFamily != advertising.AddressFamily))
                            || (addr.AddressFamily == AddressFamily.InterNetworkV6 && addr.ScopeId == 0))
                    {
                        continue;
                    }

                    var mcast = addr.AddressFamily == AddressFamily.InterNetwork
                        ? IPNetAddress.SSDPMulticastIPv4 : IPObject.IsIPv6LinkLocal(addr)
                            ? IPNetAddress.SSDPMulticastIPv6LinkLocal : IPNetAddress.SSDPMulticastIPv6SiteLocal;

                    values["HOST"] = mcast.ToString() + ":1900";

                    var message = BuildMessage(classification, values);

                    var client = _senders[ipEntry];
                    if (client != null)
                    {
                        await UdpHelper.SendMulticast((UdpProcess)client, 1900, message, sendCount ?? UdpSendCount).ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogError("Unable to find client for {0}", addr);
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
        public async Task SendSSDP(Dictionary<string, string> values, string classification, IPAddress localIP, IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            var message = BuildMessage(classification, values);

            await SendMessageAsync(message, localIP, endPoint, true).ConfigureAwait(false);
        }

        /// <summary>
        /// Restarts the service with a different set of interfaces.
        /// </summary>
        /// <param name="interfaces">A <see cref="Collection{IPObject}"/> containing a list of interfaces.</param>
        public void UpdateInterfaces(Collection<IPObject> interfaces)
        {
            if (_running)
            {
                StopServer();
                _interfaces = interfaces;
                StartServer();
            }
            else
            {
                _interfaces = interfaces;
            }
        }

        /// <summary>
        /// Updates the ssdp tracing filter.
        /// </summary>
        /// <param name="enabled">Enable tracing.</param>
        /// <param name="filter">IP filtering to use.</param>
        public void SetTracingFilter(bool enabled, string? filter = null)
        {
            var wasEnabled = Tracing;
            var oldFilter = TracingFilter;

            Tracing = enabled;

            if (!string.IsNullOrEmpty(filter))
            {
                if (IPAddress.TryParse(filter, out IPAddress? result))
                {
                    _logger.LogDebug("Filtering on: {0}", result);
                    TracingFilter = result;
                }
                else
                {
                    _logger.LogDebug("The SSDP Tracing Filter contains an invalid IP address. {0}", filter);
                }
            }
            else
            {
                _logger.LogDebug("Filtering : {0}", enabled);
            }

            if (wasEnabled != Tracing || oldFilter != TracingFilter)
            {
                UdpProcess client;
                foreach (var i in _listeners.Values)
                {
                    if (i != null)
                    {
                        client = (UdpProcess)i;
                        client.TracingFilter = TracingFilter;
                        client.Tracing = enabled;
                    }
                }

                foreach (var i in _senders.Values)
                {
                    if (i != null)
                    {
                        client = (UdpProcess)i;
                        client.TracingFilter = TracingFilter;
                        client.Tracing = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Builds an SSDP message.
        /// </summary>
        /// <param name="header">SSDP Header string.</param>
        /// <param name="values">SSDP parameters.</param>
        /// <returns>Formatted string.</returns>
        private static string BuildMessage(string header, Dictionary<string, string> values)
        {
            const string SsdpOpt = "\"http://schemas.upnp.org/upnp/1/0/\"; ns=";

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            string nc = _networkChangeCount.ToString("d2", CultureInfo.InvariantCulture);
            values["SERVER"] = HostName;
            // Optional headers.
            values["OPT"] = SsdpOpt + nc;
            values["CONFIGID.UPNP.ORG"] = "1";
            values["BOOTID.UPNP.ORG"] = values[nc + "-NLS"] = _networkLocationSignature;

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
        /// <param name="message">The message to send.</param>
        /// <param name="localIPAddress">The interface ip to use.</param>
        /// <param name="endPoint">The destination endpoint.</param>
        /// <param name="restrict">True if the transmission should be restricted to the LAN.</param>
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
                if (!_networkManager.IsInLocalNetwork(endPoint.Address))
                {
                    _logger.LogDebug("FILTERED: Sending to non-LAN address: {0}.", endPoint.Address);
                    return;
                }
            }

            var client = _senders[localIPAddress];
            if (client != null)
            {
                await UdpHelper.SendUnicast((UdpProcess)client, message, endPoint).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("Unable to find socket for {0}", localIPAddress);
            }
        }

        private void OnFailure(UdpProcess client, Exception? ex = null, string? msg = null)
        {
            _listeners.Remove(client.LocalEndPoint.Address);
            _logger.LogError(ex, msg);
        }

        /// <summary>
        /// Sends a packet via unicast.
        /// </summary>
        /// <param name="data">Packet to send.</param>
        private Dictionary<string, string> ParseMessage(string data)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
                        result.Add(propertyName, line[(i + 1)..].Trim());
                    }
                    else
                    {
                        _logger.LogDebug("{0} appears twice: {1}", propertyName, data);
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
            if (!_networkManager.IsInLocalNetwork(receivedFrom.Address))
            {
                // Not from the local LAN, so ignore it.
                return Task.CompletedTask;
            }

            string action;

            var msg = ParseMessage(data);
            action = msg["ACTION"];

            var localIpAddress = client.LocalEndPoint.Address;

            if (Tracing)
            {
                if (TracingFilter == null || TracingFilter.Equals(receivedFrom.Address) || TracingFilter.Equals(localIpAddress))
                {
                    _logger.LogDebug("<- {0} : {1} \r\n{3}", receivedFrom.Address, localIpAddress, DebugOutput(msg));
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
                    _logger.LogError(ex, "Error firing event: {0}", action);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initialises the server, and starts listening on all internal interfaces.
        /// </summary>
        private void StartServer()
        {
            if (!_running)
            {
                _running = true;
                NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

                _logger.LogDebug("EnableMultiSocketBinding : {0}", UdpHelper.EnableMultiSocketBinding);

                foreach (IPObject ip in _interfaces)
                {
                    var client = UdpHelper.CreateMulticastClient(ip.Address, 1900, ProcessMessage, _logger, OnFailure);
                    if (client != null)
                    {
                        client.TracingFilter = TracingFilter;
                        client.Tracing = Tracing;
                        _listeners[ip.Address] = client;
                    }

                    client = UdpHelper.CreateUnicastClient(ip.Address, 0, ProcessMessage, _logger, OnFailure);
                    if (client != null)
                    {
                        client.TracingFilter = TracingFilter;
                        client.Tracing = Tracing;
                        _senders[ip.Address] = client;
                    }
                }
            }
        }

        /// <summary>
        /// Stops the server and frees up resources.
        /// </summary>
        private void StopServer()
        {
            if (_running)
            {
                _running = false;
                NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                _listeners.Clear();
                _senders.Clear();
            }
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Network availability information.</param>
        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            _logger.LogDebug("Network availability changed.");
            OnNetworkChanged();
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNetworkAddressChanged(object? sender, EventArgs e)
        {
            _logger.LogDebug("Network address change detected.");
            OnNetworkChanged();
        }

        /// <summary>
        /// Async task that waits for 2 seconds before re-initialising the settings, as typically these events fire multiple times in succession.
        /// </summary>
        /// <returns>The network change async.</returns>
        private async Task OnNetworkChangeAsync()
        {
            try
            {
                await Task.Delay(2000).ConfigureAwait(false);

                // As per UPnP Device Architecture v1.0 Annex A - IPv6 Support.
                _networkLocationSignature = Guid.NewGuid().ToString();
                _networkChangeCount++;
                if (_networkChangeCount > 99)
                {
                    _networkChangeCount = 1;
                }

                if (_running)
                {
                    StopServer();
                    StartServer();
                }
            }
            finally
            {
                _eventfire = false;
            }
        }

        /// <summary>
        /// Triggers our event, and re-loads interface information.
        /// </summary>
        private void OnNetworkChanged()
        {
            lock (_eventFireLock)
            {
                if (!_eventfire)
                {
                    _logger.LogDebug("Network Address Change Event.");
                    // As network events tend to fire one after the other only fire once every second.
                    _eventfire = true;
                    _ = OnNetworkChangeAsync();
                }
            }
        }
    }
}
