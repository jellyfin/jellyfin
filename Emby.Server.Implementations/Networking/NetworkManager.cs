#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Networking
{
    /// <summary>
    /// Class to take care of network interface management.
    /// </summary>
    public class NetworkManager : INetworkManager
    {
        /// <summary>
        /// Contains the description of the interface along with its index.
        /// </summary>
        private readonly SortedList<string, int> _interfaceNames;

        /// <summary>
        /// Threading lock for network interfaces.
        /// </summary>
        private readonly object _intLock = new object();

        /// <summary>
        /// List of all interface addresses and masks.
        /// </summary>
        private readonly NetCollection _interfaceAddresses;

        /// <summary>
        /// List of all interface MAC addresses.
        /// </summary>
        private readonly List<PhysicalAddress> _macAddresses;

        private readonly ILogger _logger;

        private readonly IServerConfigurationManager _configurationManager;

        private readonly Dictionary<IPNetAddress, string> _overrideAddresses;

        /// <summary>
        /// Used to stop "event-racing conditions".
        /// </summary>
        private bool _eventfire;

        /// <summary>
        /// Unfiltered user defined LAN subnets. (Configuration.LocalNetworkSubnets).
        /// or internal interface network subnets if undefined by user.
        /// </summary>
        private NetCollection _lanSubnets;

        /// <summary>
        /// User defined list of subnets to excluded from the LAN.
        /// </summary>
        private NetCollection _excludedSubnets;

        /// <summary>
        /// Cached list of filtered addresses comprising the LAN.
        /// (_lanSubnets ?? _interfaceAddresses).Exclude(_excludedSubnets).
        /// </summary>
        private NetCollection _filteredLANSubnets;

        /// <summary>
        /// List of interface addresses to bind the WS.
        /// </summary>
        private NetCollection _bindAddresses;

        /// <summary>
        /// List of interface addresses to exclude from bind.
        /// </summary>
        private NetCollection _bindExclusions;

        /// <summary>
        /// Caches list of all internal filtered interface addresses and masks.
        /// </summary>
        private NetCollection _internalInterfaces;

        /// <summary>
        /// Flag set when _lanAddressses is set to _interfaceAddresses as no custom LAN has been defined in the config.
        /// </summary>
        private bool _usingInterfaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        /// <param name="configurationManager">IServerConfigurationManager object.</param>
        /// <param name="logger">Logger to use for messages.</param>

#pragma warning disable CS8618 // Non-nullable field is uninitialized. : Values are set in InitialiseLAN function. Compiler doesn't yet recognise this.
        public NetworkManager(IServerConfigurationManager configurationManager, ILogger<NetworkManager> logger)
        {
            _logger = logger;
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            _interfaceAddresses = new NetCollection();
            _macAddresses = new List<PhysicalAddress>();
            _interfaceNames = new SortedList<string, int>();
            _overrideAddresses = new Dictionary<IPNetAddress, string>();

            IsIP6Enabled = _configurationManager.Configuration.EnableIPV6;

            InitialiseInterfaces();
            InitialiseLAN();
            InitialiseOverrides();

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

            _configurationManager.ConfigurationUpdated += ConfigurationUpdated;

            Instance = this;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        /// <summary>
        /// Event triggered on network changes.
        /// </summary>
        public event EventHandler? NetworkChanged;

        /// <summary>
        /// Gets the singleton of this object.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. Reason: Singleton Instance
        public static NetworkManager Instance { get; internal set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        /// <inheritdoc/>
        public IPNetAddress IP4Loopback { get; } = IPNetAddress.Parse("127.0.0.1/32");

        /// <inheritdoc/>
        public IPNetAddress IP6Loopback { get; } = IPNetAddress.Parse("::1");

        /// <inheritdoc/>
        public NetCollection RemoteAddressFilter { get; private set; }

        /// <inheritdoc/>
        public bool IsIP6Enabled { get; private set; } = false;

        /// <summary>
        /// Gets a value indicating whether is multi-socket binding available.
        /// </summary>
        public bool EnableMultiSocketBinding => _configurationManager.Configuration.EnableMultiSocketBinding;

        /// <summary>
        /// Parses a string and returns a range value if possible.
        /// </summary>
        /// <param name="rangeStr">String to parse.</param>
        /// <param name="range">Range value contained in rangeStr.</param>
        /// <returns>Result of the operation.</returns>
        public static bool TryParseRange(string rangeStr, out (int min, int max) range)
        {
            if (string.IsNullOrEmpty(rangeStr))
            {
                range.min = range.max = 0; // Random Port.
                return false;
            }

            // Remove all white space.
            rangeStr = Regex.Replace(rangeStr, @"\s+", string.Empty);

            var i = rangeStr.IndexOf('-', StringComparison.OrdinalIgnoreCase);
            if (i != -1)
            {
                int minVal = int.TryParse(rangeStr.Substring(0, i), out int min) ? min : 1;
                int maxVal = int.TryParse(rangeStr.Substring(i + 1), out int max) ? max : 65535;
                if (minVal < 1)
                {
                    minVal = 1;
                }

                if (maxVal > 65535)
                {
                    maxVal = 65535;
                }

                range.max = Math.Max(minVal, maxVal);
                range.min = Math.Min(minVal, maxVal);
                return true;
            }

            if (int.TryParse(rangeStr, out int start))
            {
                if (start < 1 || start > 65535)
                {
                    start = 0; // Random Port.
                }

                range.min = range.max = start;
                return true;
            }

            // Random Port.
            range.min = range.max = 0;
            return false;
        }

        /// <inheritdoc/>
        public bool IsInSameSubnet(IPAddress subnetIP, IPAddress subnetMask, IPAddress address)
        {
            return IPObject.NetworkAddress(subnetIP, subnetMask).Equals(IPObject.NetworkAddress(address, subnetMask));
        }

        /// <inheritdoc/>
        public bool OnSameMachine(IPAddress addr1, IPAddress addr2)
        {
            if (addr1 == null || addr2 == null)
            {
                return false;
            }

            if (_configurationManager.Configuration.TrustAllIP6Interfaces && addr2.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return addr1.Equals(IP4Loopback) || addr1.Equals(IP6Loopback);
            }

            return (addr1.Equals(IP4Loopback) || addr1.Equals(IP6Loopback)) && IsInLocalNetwork(addr2);
        }

        /// <inheritdoc/>
        public void ConfigurationUpdated(object sender, EventArgs e)
        {
            // IP6 settings changed. - Needs to be public for testing.
            if (IsIP6Enabled != _configurationManager.Configuration.EnableIPV6)
            {
                InitialiseInterfaces();
                IsIP6Enabled = !IsIP6Enabled;
            }

            InitialiseLAN();
            InitialiseOverrides();
        }

        /// <inheritdoc/>
        public int GetUdpPortFromRange((int min, int max) range)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();

            // Get active udp listeners.
            var udpListenerPorts = properties.GetActiveUdpListeners()
                        .Where(n => n.Port >= range.min && n.Port <= range.max)
                        .Select(n => n.Port);

            return Enumerable.Range(range.min, range.max)
                .Where(i => !udpListenerPorts.Contains(i))
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public int GetRandomUnusedUdpPort()
        {
            // Get a port from the dynamic range.
            return GetUdpPortFromRange((49152, 65535));

            // var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // using var udpClient = new UdpClient(localEndPoint);
            // return ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        }

        /// <inheritdoc/>
        public int GetPort(string portStr)
        {
            int port = 0;
            if (TryParseRange(portStr, out (int min, int max) range))
            {
                port = GetUdpPortFromRange(range);
            }

            if (port < 0 || port > 65535)
            {
                _logger.LogError("UDP port in the range {0} cannot be allocated. Assigning random.", portStr);
                port = 0;
            }

            if (port == 0)
            {
                port = GetRandomUnusedUdpPort();
            }

            return port;
        }

        /// <inheritdoc/>
        public List<PhysicalAddress> GetMacAddresses()
        {
            // Populated in construction - so always has values.
            lock (_intLock)
            {
                return _macAddresses.ToList();
            }
        }

        /// <inheritdoc/>
        public bool IsExcluded(IPAddress ip)
        {
            return _excludedSubnets.Contains(ip);
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(string endpoint)
        {
            if (IPHost.TryParse(endpoint, out IPHost ep))
            {
                lock (_intLock)
                {
                    return _filteredLANSubnets.Contains(ep);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(IPNetAddress endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            lock (_intLock)
            {
                return _filteredLANSubnets.Contains(endpoint);
            }
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(IPAddress endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            lock (_intLock)
            {
                return _filteredLANSubnets.Contains(endpoint);
            }
        }

        /// <inheritdoc/>
        public NetCollection CreateIPCollection(string[] values, bool bracketed = false)
        {
            NetCollection col = new NetCollection();
            if (values != null)
            {
                for (int a = 0; a < values.Length; a++)
                {
                    string v = values[a].Trim();

                    try
                    {
                        if (v.StartsWith("[", StringComparison.OrdinalIgnoreCase) &&
                            v.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                        {
                            if (bracketed)
                            {
                                AddToCollection(col, v.Remove(v.Length - 1).Substring(1));
                            }
                        }
                        else
                        {
                            if (!bracketed)
                            {
                                AddToCollection(col, v);
                            }
                        }
                    }
                    catch (ArgumentException e)
                    {
                        _logger.LogInformation("Ignoring LAN value {value}. Reason : {reason}", v, e.Message);
                    }
                }
            }

            return col;
        }

        /// <inheritdoc/>
        public NetCollection GetAllBindInterfaces()
        {
            lock (_intLock)
            {
                int count = _bindAddresses.Count;

                if (count == 0)
                {
                    if (_bindExclusions.Count > 0)
                    {
                        // Return all the interfaces except the ones specifically excluded.
                        return _interfaceAddresses.Exclude(_bindExclusions);
                    }

                    // No bind address and no exclusions, so listen on all interfaces.
                    NetCollection result = new NetCollection();
                    result.Add(IPAddress.Any);
                    if (IsIP6Enabled)
                    {
                        result.Add(IPAddress.IPv6Any);
                    }

                    return result;
                }

                // Remove any excluded bind interfaces.
                return _bindAddresses.Exclude(_bindExclusions);
            }
        }

        /// <inheritdoc/>
        public string GetBindInterface(object source)
        {
            // Parse the source to see if we need to respond with an internal or external bind interface.
            IPObject sourceAddr;
            if (source is string sourceStr && !string.IsNullOrEmpty(sourceStr))
            {
                if (IPHost.TryParse(sourceStr, out IPHost host))
                {
                    sourceAddr = host;
                }
                else
                {
                    // Assume it's external, as we might not be able to resolve the host.
                    sourceAddr = IPHost.None;
                }
            }
            else if (source is IPAddress sourceIP)
            {
                sourceAddr = new IPNetAddress(sourceIP);
            }
            else
            {
                // If we get nothing assume external.
                sourceAddr = IPHost.None;
            }

            bool haveSource = !sourceAddr.Address.Equals(IPAddress.None);

            if (haveSource && !IsIP6Enabled && sourceAddr.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _logger.LogWarning("IPv6 disabled in jellyfin, but enabled in OS. This may affect how the interface is selected.");
            }

            bool isExternal = haveSource && !IsLANAddressRange(sourceAddr);

            string bindPreference = string.Empty;
            if (haveSource)
            {
                // Check for user override.
                foreach (var addr in _overrideAddresses)
                {
                    if (addr.Key.Equals(IPAddress.Broadcast))
                    {
                        bindPreference = addr.Value;
                        break;
                    }
                    else if ((addr.Key.Equals(IPAddress.Any) || addr.Key.Equals(IPAddress.IPv6Any)) && isExternal)
                    {
                        bindPreference = addr.Value;
                        break;
                    }
                    else if (addr.Key.Contains(sourceAddr))
                    {
                        bindPreference = addr.Value;
                        break;
                    }
                }
            }

            _logger.LogDebug("GetBindInterface: Souce: {0}, External: {1}:", haveSource, isExternal);

            if (!string.IsNullOrEmpty(bindPreference))
            {
                _logger.LogInformation("Using BindAddress {0}", bindPreference);
                return bindPreference;
            }

            string ipresult;
            // No preference given, so auto select the best.
            lock (_intLock)
            {
                NetCollection nc = _bindAddresses.Exclude(_bindExclusions);

                int count = nc.Count;
                if (count == 1 && (_bindAddresses[0].Equals(IPAddress.Any) || _bindAddresses.Equals(IPAddress.IPv6Any)))
                {
                    // Ignore IPAny addresses.
                    count = 0;
                }

                if (count != 0)
                {
                    if (haveSource)
                    {
                        // Does the request originate in one of the interface subnets?
                        // (For systems with multiple internal network cards, and multiple subnets)
                        foreach (var intf in nc)
                        {
                            if (intf.Contains(sourceAddr))
                            {
                                ipresult = intf.Address.ToString();
                                _logger.LogDebug("GetBindInterface: Has source, matched user defined interface on range. {0}", ipresult);
                                return ipresult;
                            }
                        }
                    }

                    // Check to see if any of the bind interfaces are in the same subnet.
                    var ncRes = nc.Where(p => !IsLANAddressRange(p) && !p.IsLoopback())
                        .OrderBy(p => p.Tag);

                    if (ncRes.Any())
                    {
                        ipresult = ncRes.First().Address.ToString();
                        _logger.LogDebug("GetBindInterface: Has source, select best user defined interface. {0}", ipresult);
                        return ipresult;
                    }

                    ipresult = nc[0].Address.ToString();
                    _logger.LogDebug("GetBindInterface: Selected first user defined interface.", ipresult);
                    return ipresult;
                }

                if (isExternal)
                {
                    // Get the first LAN interface address that isn't a loopback.
                    var extResult = _interfaceAddresses
                        .Exclude(_bindExclusions)
                        .Where(p => !IsLANAddressRange(p) && !p.IsLoopback())
                        .OrderBy(p => p.Tag);

                    if (extResult.Any())
                    {
                        if (haveSource)
                        {
                            // Does the request originate in one of the interface subnets?
                            // (For systems with multiple internal network cards, and multiple subnets)
                            foreach (var intf in extResult)
                            {
                                if (!IsLANAddressRange(intf) && intf.Contains(sourceAddr))
                                {
                                    ipresult = intf.Address.ToString();
                                    _logger.LogDebug("GetBindInterface: Selected best external on interface on range. {0}", ipresult);
                                    return ipresult;
                                }
                            }
                        }

                        ipresult = extResult.First().Address.ToString();
                        _logger.LogDebug("GetBindInterface: Selected first external interface. {0}", ipresult);
                        return ipresult;
                    }

                    // Have to return something, so return an internal address
                }

                // Get the first LAN interface address that isn't a loopback.
                var result = _interfaceAddresses
                    .Exclude(_bindExclusions)
                    .Where(p => IsLANAddressRange(p) && !p.IsLoopback())
                    .OrderBy(p => p.Tag);

                if (result.Any())
                {
                    if (haveSource)
                    {
                        // Does the request originate in one of the interface subnets?
                        // (For systems with multiple internal network cards, and multiple subnets)
                        foreach (var intf in result)
                        {
                            if (IsLANAddressRange(intf) && intf.Contains(sourceAddr))
                            {
                                ipresult = intf.Address.ToString();
                                _logger.LogDebug("GetBindInterface: Has source, matched best internal interface on range. {0}", ipresult);
                                return ipresult;
                            }
                        }
                    }

                    ipresult = result.First().Address.ToString();
                    _logger.LogDebug("GetBindInterface: Matched first internal interface. {0}", ipresult);
                    return ipresult;
                }

                // There isn't any others, so we'll use the loopback.
                ipresult = "127.0.0.1";
                _logger.LogDebug("GetBindInterface: Default return. {0}", ipresult);
                return ipresult;
            }
        }

        /// <inheritdoc/>
        public NetCollection GetInternalBindAddresses()
        {
            lock (_intLock)
            {
                int count = _bindAddresses.Count;

                if (count == 0)
                {
                    if (_bindExclusions.Count > 0)
                    {
                        // Return all the internal interfaces except the ones excluded.
                        return new NetCollection(_internalInterfaces.Where(p => !_bindExclusions.Contains(p)));
                    }

                    // No bind address, so return all internal interfaces.
                    return _internalInterfaces;
                }

                return _bindAddresses;
            }
        }

        /// <inheritdoc/>
        public bool IsLANAddressRange(IPObject address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // See conversation at https://github.com/jellyfin/jellyfin/pull/3515.
            if (_configurationManager.Configuration.TrustAllIP6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }

            // As private addresses can be redefined by Configuration.LocalNetworkAddresses
            return _filteredLANSubnets.Contains(address);
        }

        /// <inheritdoc/>
        public bool IsPrivateAddressRange(IPObject address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // See conversation at https://github.com/jellyfin/jellyfin/pull/3515.
            if (_configurationManager.Configuration.TrustAllIP6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }
            else
            {
                return address.IsPrivateAddressRange();
            }
        }

        /// <inheritdoc/>
        public bool IsExcludedInterface(IPAddress address)
        {
            lock (_intLock)
            {
                if (_bindExclusions.Count > 0)
                {
                    return _bindExclusions.Contains(address);
                }

                return false;
            }
        }

        /// <inheritdoc/>
        public NetCollection GetFilteredLANSubnets(NetCollection? filter = null)
        {
            lock (_intLock)
            {
                if (filter == null)
                {
                    return new NetCollection(_filteredLANSubnets);
                }

                return _lanSubnets.Exclude(filter);
            }
        }

        /// <inheritdoc/>
        public bool IsValidInterfaceAddress(IPAddress address)
        {
            lock (_intLock)
            {
                return _interfaceAddresses.Contains(address);
            }
        }

        /// <inheritdoc/>
        public bool TryParseInterface(string token, out IPNetAddress result)
        {
            if (string.IsNullOrEmpty(token))
            {
                result = IPNetAddress.None;
                return false;
            }

            if (_interfaceNames != null && _interfaceNames.TryGetValue(token.ToLower(CultureInfo.InvariantCulture), out int index))
            {
                _logger.LogInformation("Interface {0} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (Math.Abs(iface.Tag) == index
                        && ((!IsIP6Enabled && iface.Address.AddressFamily == AddressFamily.InterNetwork) || IsIP6Enabled))
                    {
                        result = iface;
                        return true;
                    }
                }
            }

            return IPNetAddress.TryParse(token, out result);
        }

        /// <summary>
        /// Parses strings into the collection, replacing any interface references.
        /// </summary>
        /// <param name="col">Collection.</param>
        /// <param name="token">String to parse.</param>
        private void AddToCollection(NetCollection col, string token)
        {
            // Is it the name of an interface (windows) eg, Wireless LAN adapter Wireless Network Connection 1.
            // Null check required here for automated testing.
            if (_interfaceNames != null && _interfaceNames.TryGetValue(token.ToLower(CultureInfo.InvariantCulture), out int index))
            {
                _logger.LogInformation("Interface {0} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (Math.Abs(iface.Tag) == index
                        && ((!IsIP6Enabled && iface.Address.AddressFamily == AddressFamily.InterNetwork) || IsIP6Enabled))
                    {
                        col.Add(iface);
                    }
                }
            }
            else if (NetCollection.TryParse(token, out IPObject obj))
            {
                if (!IsIP6Enabled)
                {
                    // Remove IP6 addresses from multi-homed IPHosts.
                    obj.RemoveIP6();
                    if (!obj.IsIP6())
                    {
                        col.Add(obj);
                    }
                }
                else
                {
                    col.Add(obj);
                }
            }
            else
            {
                _logger.LogDebug("Invalid or unknown network {0}.", token);
            }
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Network availablity information.</param>
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _logger.LogDebug("NetworkAvailabilityChanged");
            OnNetworkChanged();
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            _logger.LogDebug("NetworkAddressChanged");
            OnNetworkChanged();
        }

        /// <summary>
        /// Async task that waits for 2 seconds before re-initialising the settings.
        /// </summary>
        /// <returns>The network change async.</returns>
        private async Task OnNetworkChangeAsync()
        {
            try
            {
                await Task.Delay(2000).ConfigureAwait(false);
                InitialiseInterfaces();
                // Recalculate LAN caches.
                InitialiseLAN();

                NetworkChanged?.Invoke(this, EventArgs.Empty);
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
            if (!_eventfire)
            {
                // As network events tend to fire one after the other only fire once every second.
                _eventfire = true;
                _ = OnNetworkChangeAsync();
            }
        }

        /// <summary>
        /// Parses the user defined overrides into the dictionary object.
        /// Overrides are the equivalent of localised publishedServerUrl, enabling
        /// different addresses to be advertised over different subnets.
        /// format is subnet=ipaddress|host|uri
        /// when subnet = 0.0.0.0, any external address matches.
        /// </summary>
        private void InitialiseOverrides()
        {
            string[] overrides = _configurationManager.Configuration.PublishedServerUriBySubnet;
            if (overrides == null)
            {
                lock (_intLock)
                {
                    _overrideAddresses.Clear();
                }

                return;
            }

            lock (_intLock)
            {
                _overrideAddresses.Clear();

                foreach (var entry in overrides)
                {
                    int i = entry.IndexOf("=", StringComparison.OrdinalIgnoreCase);
                    if (i == -1)
                    {
                        _logger.LogError("Unable to parse bind override. {0}", entry);
                    }
                    else
                    {
                        if (TryParseInterface(entry.Substring(0, i), out IPNetAddress address))
                        {
                            _overrideAddresses[address] = entry.Substring(i + 1).Trim();
                        }
                        else
                        {
                            _logger.LogError("Unable to parse bind ip address. {0}", entry);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialises internal LAN cache settings.
        /// </summary>
        private void InitialiseLAN()
        {
            lock (_intLock)
            {
                string[] ba = _configurationManager.Configuration.LocalNetworkAddresses;

                // TODO: remove when bug fixed: https://github.com/jellyfin/jellyfin-web/issues/1334

                if (ba.Length == 1 && ba[0].IndexOf(',', StringComparison.OrdinalIgnoreCase) != -1)
                {
                    ba = ba[0].Split(',');
                }

                // TODO: end fix.

                // Read and parse bind addresses and exclusions, removing ones that don't exist.
                _bindAddresses = CreateIPCollection(ba).Union(_interfaceAddresses);
                _bindExclusions = CreateIPCollection(ba, true).Union(_interfaceAddresses);
                RemoteAddressFilter = CreateIPCollection(_configurationManager.Configuration.RemoteIPFilter);

                _logger.LogDebug("Refreshing LAN information.");

                // Get config options.
                string[] subnets = _configurationManager.Configuration.LocalNetworkSubnets;

                // Create lists from user settings.

                _lanSubnets = CreateIPCollection(subnets);
                _excludedSubnets = CreateIPCollection(subnets, true);

                // If no LAN addresses are specified - all interface subnets are deemed to be the LAN
                _usingInterfaces = _lanSubnets.Count == 0;

                // NOTE: The order of the commands in this statement matters.
                if (_usingInterfaces)
                {
                    _logger.LogDebug("Using LAN interface addresses as user provided no LAN details.");
                    // Internal interfaces must be private and not excluded.
                    _internalInterfaces = new NetCollection(
                        _interfaceAddresses
                        .Where(i => IsPrivateAddressRange(i) && !_excludedSubnets.Contains(i)));

                    // Subnets are the same as the calculated internal interface.
                    _lanSubnets = NetCollection.AsNetworks(_internalInterfaces);

                    // We must listen on loopback for LiveTV to function regardless of the settings.
                    if (IsIP6Enabled)
                    {
                        _lanSubnets.Add(IP6Loopback);
                    }

                    _lanSubnets.Add(IP4Loopback);

                    // Filtered LAN subnets are subnets of the LAN Addresses.
                    _filteredLANSubnets = NetCollection.AsNetworks(_lanSubnets.Exclude(_excludedSubnets));
                }
                else
                {
                    // We must listen on loopback for LiveTV to function regardless of the settings.
                    if (IsIP6Enabled)
                    {
                        _lanSubnets.Add(IP6Loopback);
                    }

                    _lanSubnets.Add(IP4Loopback);

                    // Filtered LAN subnets are subnets of the LAN Addresses.
                    _filteredLANSubnets = NetCollection.AsNetworks(_lanSubnets.Exclude(_excludedSubnets));

                    // Internal interfaces must be private, not excluded and part of the LocalNetworkSubnet.
                    _internalInterfaces = new NetCollection(_interfaceAddresses
                        .Where(i => IsLANAddressRange(i) &&
                            !_excludedSubnets.Contains(i) &&
                            _lanSubnets.Contains(i)));
                }

                _logger.LogInformation("Defined LAN addresses : {0}", _lanSubnets);
                _logger.LogInformation("Defined LAN exclusions : {0}", _excludedSubnets);
                _logger.LogInformation("Using LAN addresses: {0}", _filteredLANSubnets);
                _logger.LogInformation("Using bind addresses: {0}", _bindAddresses);
                _logger.LogInformation("Using bind exclusions: {0}", _bindExclusions);
            }
        }

        /// <summary>
        /// Generate a list of all the interface ip addresses and submasks where that are in the active/unknown state.
        /// Generate a list of all active mac addresses that aren't loopback addreses.
        /// </summary>
        private void InitialiseInterfaces()
        {
            lock (_intLock)
            {
                _logger.LogDebug("Refreshing interfaces.");

                _interfaceNames.Clear();
                _interfaceAddresses.Clear();

                // Retrieve a list of network interfaces that are up or unknown. (See link for the reason for unknown)
                // https://stackoverflow.com/questions/17868420/networkinterface-getallnetworkinterfaces-returns-interfaces-with-operationalst

                try
                {
                    bool ip6 = _configurationManager.Configuration.EnableIPV6;

                    IEnumerable<NetworkInterface> nics = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(x => x.OperationalStatus == OperationalStatus.Up
                            || x.OperationalStatus == OperationalStatus.Unknown);

                    foreach (NetworkInterface adapter in nics)
                    {
                        try
                        {
                            IPInterfaceProperties ipProperties = adapter.GetIPProperties();
                            PhysicalAddress mac = adapter.GetPhysicalAddress();

                            // populate mac list
                            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && mac != null && mac != PhysicalAddress.None)
                            {
                                _macAddresses.Add(mac);
                            }

                            // populate interface address list
                            foreach (UnicastIPAddressInformation info in ipProperties.UnicastAddresses)
                            {
                                if (info.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    IPNetAddress nw = new IPNetAddress(info.Address, info.IPv4Mask)
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv4Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    /* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
                                    if (ipProperties.GatewayAddresses.Count > 0 || ipProperties.DnsAddresses.Count > 0)
                                    {
                                        // -ve Tags signify the interface has a gateway.
                                        nw.Tag *= -1;
                                    }

                                    _interfaceAddresses.Add(nw);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Description.ToLower(CultureInfo.InvariantCulture)] = tag;
                                    _interfaceNames["eth" + tag.ToString(CultureInfo.InvariantCulture)] = tag;
                                }
                                else if (ip6 && info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    IPNetAddress nw = new IPNetAddress(info.Address)
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv6Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    /* Mono on OSX doesn't give any gateway addresses, so check DNS entries */
                                    if (ipProperties.GatewayAddresses.Count > 0 || ipProperties.DnsAddresses.Count > 0)
                                    {
                                        // -ve Tags signify the interface has a gateway.
                                        nw.Tag *= -1;
                                    }

                                    _interfaceAddresses.Add(nw);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Description.ToLower(CultureInfo.InvariantCulture)] = tag;
                                    _interfaceNames["eth" + tag.ToString(CultureInfo.InvariantCulture)] = tag;
                                }
                            }
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            // Ignore error, and attempt to continue.
                        }
#pragma warning restore CA1031 // Do not catch general exception types
                    }

                    _logger.LogDebug("Discovered {0} interfaces.", _interfaceAddresses.Count);

                    // If for some reason we don't have an interface info, resolve our DNS name.
                    if (_interfaceAddresses.Count == 0)
                    {
                        _logger.LogWarning("No interfaces information available. Using loopback.");

                        IPHost host = new IPHost(Dns.GetHostName());
                        foreach (var a in host.GetAddresses())
                        {
                            _interfaceAddresses.Add(a);
                        }

                        if (_interfaceAddresses.Count == 0)
                        {
                            _logger.LogError("No interfaces information available. Resolving DNS name.");
                            // Last ditch attempt - use loopback address.
                            _interfaceAddresses.Add(IP4Loopback);
                            if (IsIP6Enabled)
                            {
                                _interfaceAddresses.Add(IP6Loopback);
                            }
                        }
                    }
                }
                catch (NetworkInformationException ex)
                {
                    _logger.LogError(ex, "Error in InitialiseInterfaces.");
                }
            }
        }
    }
}
