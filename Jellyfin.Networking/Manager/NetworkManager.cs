#pragma warning disable CA1021 // Avoid out parameters

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Jellyfin.Networking.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NetworkCollection;
using NetworkCollection.Udp;

namespace Jellyfin.Networking.Manager
{
    /// <summary>
    /// Class to take care of network interface management.
    /// </summary>
    public class NetworkManager : INetworkManager, IDisposable
    {
        /// <summary>
        /// Contains the description of the interface along with its index.
        /// </summary>
        private readonly Dictionary<string, int> _interfaceNames;

        /// <summary>
        /// Threading lock for network properties.
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

        private readonly ILogger<NetworkManager> _logger;

        private readonly IConfigurationManager _configurationManager;

        private readonly object _eventFireLock;

        /// <summary>
        /// Holds the bind address overrides.
        /// </summary>
        private readonly Dictionary<IPNetAddress, string> _publishedServerUrls;

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
        /// Flag set when no custom LAN has been defined in the config.
        /// </summary>
        private bool _usingPrivateAddresses;

        /// <summary>
        /// True if this object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        /// <param name="configurationManager">IServerConfigurationManager instance.</param>
        /// <param name="logger">Logger to use for messages.</param>
#pragma warning disable CS8618 // Non-nullable field is uninitialized. : Values are set in UpdateSettings function. Compiler doesn't yet recognise this.
        public NetworkManager(IConfigurationManager configurationManager, ILogger<NetworkManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));

            _interfaceAddresses = new NetCollection();
            _macAddresses = new List<PhysicalAddress>();
            _interfaceNames = new Dictionary<string, int>();
            _publishedServerUrls = new Dictionary<IPNetAddress, string>();
            _eventFireLock = new object();

            UpdateSettings(_configurationManager.GetNetworkConfiguration());

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

            _configurationManager.NamedConfigurationUpdated += ConfigurationUpdated;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        /// <summary>
        /// Event triggered on network changes.
        /// </summary>
        public event EventHandler? NetworkChanged;

        /// <summary>
        /// Gets or sets a value indicating whether testing is taking place.
        /// </summary>
        public static string MockNetworkSettings { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether IP6 is enabled.
        /// </summary>
        public bool IsIP6Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IP4 is enabled.
        /// </summary>
        public bool IsIP4Enabled { get; set; }

        /// <inheritdoc/>
        public NetCollection RemoteAddressFilter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether is all IPv6 interfaces are trusted as internal.
        /// </summary>
        public bool TrustAllIP6Interfaces { get; internal set; }

        /// <summary>
        /// Gets the Published server override list.
        /// </summary>
        public Dictionary<IPNetAddress, string> PublishedServerUrls => _publishedServerUrls;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public List<PhysicalAddress> GetMacAddresses()
        {
            // Populated in construction - so always has values.
            return _macAddresses.ToList();
        }

        /// <inheritdoc/>
        public bool IsGatewayInterface(object? addressObj)
        {
            var address = addressObj switch
            {
                IPAddress addressIp => addressIp,
                IPObject addressIpObj => addressIpObj.Address,
                _ => IPAddress.None
            };

            return _internalInterfaces.Any(i => i.Address.Equals(address) && i.Tag < 0);
        }

        /// <inheritdoc/>
        public NetCollection GetLoopbacks()
        {
            NetCollection nc = new NetCollection();
            if (IsIP4Enabled)
            {
                nc.Add(IPAddress.Loopback);
            }

            if (IsIP6Enabled)
            {
                nc.Add(IPAddress.IPv6Loopback);
            }

            return nc;
        }

        /// <inheritdoc/>
        public bool IsExcluded(IPAddress ip)
        {
            return _excludedSubnets.ContainsAddress(ip);
        }

        /// <inheritdoc/>
        public bool IsExcluded(EndPoint ip)
        {
            return ip != null && IsExcluded(((IPEndPoint)ip).Address);
        }

        /// <inheritdoc/>
        public NetCollection CreateIPCollection(string[] values, bool bracketed = false)
        {
            NetCollection col = new NetCollection();
            if (values == null)
            {
                return col;
            }

            for (int a = 0; a < values.Length; a++)
            {
                string v = values[a].Trim();

                try
                {
                    if (v.StartsWith('[') && v.EndsWith(']'))
                    {
                        if (bracketed)
                        {
                            AddToCollection(col, v[1..^1]);
                        }
                    }
                    else if (v.StartsWith('!'))
                    {
                        if (bracketed)
                        {
                            AddToCollection(col, v.Substring(1));
                        }
                    }
                    else if (!bracketed)
                    {
                        AddToCollection(col, v);
                    }
                }
                catch (ArgumentException e)
                {
                    _logger.LogInformation("Ignoring LAN value {value}. Reason : {reason}", v, e.Message);
                }
            }

            return col;
        }

        /// <inheritdoc/>
        public NetCollection GetAllBindInterfaces(bool individualInterfaces = false)
        {
            int count = _bindAddresses.Count;

            if (count == 0)
            {
                if (_bindExclusions.Count > 0)
                {
                    // Return all the interfaces except the ones specifically excluded.
                    return _interfaceAddresses.Exclude(_bindExclusions);
                }

                if (individualInterfaces)
                {
                    return new NetCollection(_interfaceAddresses);
                }

                // No bind address and no exclusions, so listen on all interfaces.
                NetCollection result = new NetCollection();

                if (IsIP4Enabled)
                {
                    result.Add(IPAddress.Any);
                }

                if (IsIP6Enabled)
                {
                    result.Add(IPAddress.IPv6Any);
                }

                return result;
            }

            // Remove any excluded bind interfaces.
            return _bindAddresses.Exclude(_bindExclusions);
        }

        /// <inheritdoc/>
        public string GetBindInterface(string source, out int? port)
        {
            if (!string.IsNullOrEmpty(source) && IPHost.TryParse(source, out IPHost host))
            {
                return GetBindInterface(host, out port);
            }

            return GetBindInterface(IPHost.None, out port);
        }

        /// <inheritdoc/>
        public string GetBindInterface(IPAddress source, out int? port)
        {
            return GetBindInterface(new IPNetAddress(source), out port);
        }

        /// <inheritdoc/>
        public string GetBindInterface(HttpRequest source, out int? port)
        {
            string result;

            if (source != null && IPHost.TryParse(source.Host.Host, out IPHost host))
            {
                result = GetBindInterface(host, out port);
                port ??= source.Host.Port;
            }
            else
            {
                result = GetBindInterface(IPNetAddress.None, out port);
                port ??= source?.Host.Port;
            }

            return result;
        }

        /// <inheritdoc/>
        public string GetBindInterface(IPObject source, out int? port)
        {
            port = null;
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Do we have a source?
            bool haveSource = !source.Address.Equals(IPAddress.None);
            bool isExternal = false;

            if (haveSource)
            {
                if (!IsIP6Enabled && source.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _logger.LogWarning("IPv6 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
                }

                if (!IsIP4Enabled && source.AddressFamily == AddressFamily.InterNetwork)
                {
                    _logger.LogWarning("IPv4 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
                }

                isExternal = !IsInLocalNetwork(source);

                if (MatchesPublishedServerUrl(source, isExternal, out string res, out port))
                {
                    _logger.LogInformation("{0}: Using BindAddress {1}:{2}", source, res, port);
                    return res;
                }
            }

            _logger.LogDebug("GetBindInterface: Source: {0}, External: {1}:", haveSource, isExternal);

            // No preference given, so move on to bind addresses.
            if (MatchesBindInterface(source, isExternal, out string result))
            {
                return result;
            }

            if (isExternal && MatchesExternalInterface(source, out result))
            {
                return result;
            }

            // Get the first LAN interface address that isn't a loopback.
            var interfaces = new NetCollection(_interfaceAddresses
                .Exclude(_bindExclusions)
                .Where(p => IsInLocalNetwork(p))
                .OrderBy(p => p.Tag));

            if (interfaces.Count > 0)
            {
                if (haveSource)
                {
                    // Does the request originate in one of the interface subnets?
                    // (For systems with multiple internal network cards, and multiple subnets)
                    foreach (var intf in interfaces)
                    {
                        if (intf.Contains(source))
                        {
                            result = FormatIP6String(intf.Address);
                            _logger.LogDebug("{0}: GetBindInterface: Has source, matched best internal interface on range. {1}", source, result);
                            return result;
                        }
                    }
                }

                result = FormatIP6String(interfaces.First().Address);
                _logger.LogDebug("{0}: GetBindInterface: Matched first internal interface. {1}", source, result);
                return result;
            }

            // There isn't any others, so we'll use the loopback.
            result = IsIP6Enabled ? "::" : "127.0.0.1";
            _logger.LogWarning("{0}: GetBindInterface: Loopback return.", source, result);
            return result;
        }

        /// <inheritdoc/>
        public NetCollection GetInternalBindAddresses()
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
                return new NetCollection(_internalInterfaces.Where(p => !p.IsLoopback()));
            }

            return new NetCollection(_bindAddresses);
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(IPObject address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.Equals(IPAddress.None))
            {
                return false;
            }

            // See conversation at https://github.com/jellyfin/jellyfin/pull/3515.
            if (TrustAllIP6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }

            // As private addresses can be redefined by Configuration.LocalNetworkAddresses
            return _lanSubnets.Contains(address) && !_excludedSubnets.Contains(address);
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(string address)
        {
            if (IPHost.TryParse(address, out IPHost ep))
            {
                return _lanSubnets.Contains(ep) && !_excludedSubnets.Contains(ep);
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // See conversation at https://github.com/jellyfin/jellyfin/pull/3515.
            if (TrustAllIP6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return true;
            }

            // As private addresses can be redefined by Configuration.LocalNetworkAddresses
            return _lanSubnets.ContainsAddress(address) && !_excludedSubnets.ContainsAddress(address);
        }

        /// <inheritdoc/>
        public bool IsPrivateAddressRange(IPObject address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            // See conversation at https://github.com/jellyfin/jellyfin/pull/3515.
            if (TrustAllIP6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
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
            return _bindExclusions.ContainsAddress(address);
        }

        /// <inheritdoc/>
        public NetCollection GetFilteredLANSubnets(NetCollection? filter = null)
        {
            if (filter == null)
            {
                return _lanSubnets.Exclude(_excludedSubnets).AsNetworks();
            }

            return _lanSubnets.Exclude(filter);
        }

        /// <inheritdoc/>
        public bool IsValidInterfaceAddress(IPAddress address)
        {
            return _interfaceAddresses.ContainsAddress(address);
        }

        /// <inheritdoc/>
        public bool TryParseInterface(string token, out NetCollection? result)
        {
            result = null;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (_interfaceNames != null && _interfaceNames.TryGetValue(token.ToLower(CultureInfo.InvariantCulture), out int index))
            {
                result = new NetCollection();

                _logger.LogInformation("Interface {0} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (Math.Abs(iface.Tag) == index &&
                        ((IsIP4Enabled && iface.Address.AddressFamily == AddressFamily.InterNetwork) ||
                         (IsIP6Enabled && iface.Address.AddressFamily == AddressFamily.InterNetworkV6)))
                    {
                        result.Add(iface);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Reloads all settings and re-initialises the instance.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        public void UpdateSettings(object configuration)
        {
            NetworkConfiguration config = (NetworkConfiguration)configuration ?? throw new ArgumentNullException(nameof(configuration));

            IsIP4Enabled = Socket.OSSupportsIPv4 && config.EnableIPV4;
            IsIP6Enabled = Socket.OSSupportsIPv6 && config.EnableIPV6;

            if (!IsIP6Enabled && !IsIP4Enabled)
            {
                _logger.LogError("IPv4 and IPv6 cannot both be disabled.");
                IsIP4Enabled = true;
            }

            TrustAllIP6Interfaces = config.TrustAllIP6Interfaces;
            UdpHelper.EnableMultiSocketBinding = config.EnableMultiSocketBinding;

            if (string.IsNullOrEmpty(MockNetworkSettings))
            {
                InitialiseInterfaces();
            }
            else // Used in testing only.
            {
                // Format is <IPAddress>,<Index>,<Name>: <next interface>. Set index to -ve to simulate a gateway.
                var interfaceList = MockNetworkSettings.Split(':');
                foreach (var details in interfaceList)
                {
                    var parts = details.Split(',');
                    var address = IPNetAddress.Parse(parts[0]);
                    var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    address.Tag = index;
                    _interfaceAddresses.Add(address);
                    _interfaceNames.Add(parts[2], Math.Abs(index));
                }
            }

            InitialiseLAN(config);
            InitialiseBind(config);
            InitialiseRemote(config);
            InitialiseOverrides(config);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True to dispose the managed state.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _configurationManager.NamedConfigurationUpdated -= ConfigurationUpdated;
                    NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                    NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Trys to identify the string and return an object of that class.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <param name="result">IPObject to return.</param>
        /// <returns>True if the value parsed successfully.</returns>
        private static bool TryParse(string addr, out IPObject result)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                // Is it an IP address
                if (IPNetAddress.TryParse(addr, out IPNetAddress nw))
                {
                    result = nw;
                    return true;
                }

                if (IPHost.TryParse(addr, out IPHost h))
                {
                    result = h;
                    return true;
                }
            }

            result = IPNetAddress.None;
            return false;
        }

        /// <summary>
        /// Converts an IPAddress into a string.
        /// Ipv6 addresses are returned in [ ], with their scope removed.
        /// </summary>
        /// <param name="address">Address to convert.</param>
        /// <returns>URI save conversion of the address.</returns>
        private static string FormatIP6String(IPAddress address)
        {
            var str = address.ToString();
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                int i = str.IndexOf("%", StringComparison.OrdinalIgnoreCase);

                if (i != -1)
                {
                    str = str.Substring(0, i);
                }

                return $"[{str}]";
            }

            return str;
        }

        private void ConfigurationUpdated(object? sender, ConfigurationUpdateEventArgs evt)
        {
            if (evt.Key.Equals("network", StringComparison.Ordinal))
            {
                UpdateSettings(evt.NewConfiguration);
            }
        }

        /// <summary>
        /// Checks the string to see if it matches any interface names.
        /// </summary>
        /// <param name="token">String to check.</param>
        /// <param name="index">Interface index number.</param>
        /// <returns>True if an interface name matches the token.</returns>
        private bool IsInterface(string token, out int index)
        {
            index = -1;

            // Is it the name of an interface (windows) eg, Wireless LAN adapter Wireless Network Connection 1.
            // Null check required here for automated testing.
            if (_interfaceNames != null && token.Length > 1)
            {
                bool partial = token[^1] == '*';
                if (partial)
                {
                    token = token[0..^1];
                }

                foreach ((string interfc, int interfcIndex) in _interfaceNames)
                {
                    if ((!partial && string.Equals(interfc, token, StringComparison.OrdinalIgnoreCase)) ||
                       (partial && interfc.StartsWith(token, true, CultureInfo.InvariantCulture)))
                    {
                        index = interfcIndex;
                        return true;
                    }
                }
            }

            return false;
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
            if (IsInterface(token, out int index))
            {
                _logger.LogInformation("Interface {0} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (Math.Abs(iface.Tag) == index &&
                        ((IsIP4Enabled && iface.Address.AddressFamily == AddressFamily.InterNetwork) ||
                         (IsIP6Enabled && iface.Address.AddressFamily == AddressFamily.InterNetworkV6)))
                    {
                        col.Add(iface);
                    }
                }
            }
            else if (TryParse(token, out IPObject obj))
            {
                if (!IsIP6Enabled)
                {
                    // Remove IP6 addresses from multi-homed IPHosts.
                    obj.Remove(AddressFamily.InterNetworkV6);
                    if (!obj.IsIP6())
                    {
                        col.Add(obj);
                    }
                }
                else if (!IsIP4Enabled)
                {
                    // Remove IP4 addresses from multi-homed IPHosts.
                    obj.Remove(AddressFamily.InterNetwork);
                    if (obj.IsIP6())
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
                InitialiseInterfaces();
                // Recalculate LAN caches.
                InitialiseLAN(_configurationManager.GetNetworkConfiguration());

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
            lock (_eventFireLock)
            {
                if (!_eventfire)
                {
                    _logger.LogDebug("Network Address Change Event.");
                    // As network events tend to fire one after the other only fire once every second.
                    _eventfire = true;
                    OnNetworkChangeAsync().GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Parses the user defined overrides into the dictionary object.
        /// Overrides are the equivalent of localised publishedServerUrl, enabling
        /// different addresses to be advertised over different subnets.
        /// format is subnet=ipaddress|host|uri
        /// when subnet = 0.0.0.0, any external address matches.
        /// </summary>
        private void InitialiseOverrides(NetworkConfiguration config)
        {
            lock (_intLock)
            {
                _publishedServerUrls.Clear();
                string[] overrides = config.PublishedServerUriBySubnet;
                if (overrides == null)
                {
                    return;
                }

                foreach (var entry in overrides)
                {
                    var parts = entry.Split('=');
                    if (parts.Length != 2)
                    {
                        _logger.LogError("Unable to parse bind override. {0}", entry);
                    }
                    else
                    {
                        var replacement = parts[1].Trim();
                        if (string.Equals(parts[0], "remaining", StringComparison.OrdinalIgnoreCase))
                        {
                            _publishedServerUrls[new IPNetAddress(IPAddress.Broadcast)] = replacement;
                        }
                        else if (string.Equals(parts[0], "external", StringComparison.OrdinalIgnoreCase))
                        {
                            _publishedServerUrls[new IPNetAddress(IPAddress.Any)] = replacement;
                        }
                        else if (TryParseInterface(parts[0], out NetCollection? addresses) && addresses != null)
                        {
                            foreach (IPNetAddress na in addresses)
                            {
                                _publishedServerUrls[na] = replacement;
                            }
                        }
                        else if (IPNetAddress.TryParse(parts[0], out IPNetAddress result))
                        {
                            _publishedServerUrls[result] = replacement;
                        }
                        else
                        {
                            _logger.LogError("Unable to parse bind ip address. {0}", parts[1]);
                        }
                    }
                }
            }
        }

        private void InitialiseBind(NetworkConfiguration config)
        {
            string[] lanAddresses = config.LocalNetworkAddresses;

            // TODO: remove when bug fixed: https://github.com/jellyfin/jellyfin-web/issues/1334

            if (lanAddresses.Length == 1 && lanAddresses[0].IndexOf(',', StringComparison.OrdinalIgnoreCase) != -1)
            {
                lanAddresses = lanAddresses[0].Split(',');
            }

            // TODO: end fix: https://github.com/jellyfin/jellyfin-web/issues/1334

            // Add virtual machine interface names to the list of bind exclusions, so that they are auto-excluded.
            if (config.IgnoreVirtualInterfaces)
            {
                var newList = lanAddresses.ToList();
                newList.AddRange(config.VirtualInterfaceNames.Split(',').ToList());
                lanAddresses = newList.ToArray();
            }

            // Read and parse bind addresses and exclusions, removing ones that don't exist.
            _bindAddresses = CreateIPCollection(lanAddresses).Union(_interfaceAddresses);
            _bindExclusions = CreateIPCollection(lanAddresses, true).Union(_interfaceAddresses);
            _logger.LogInformation("Using bind addresses: {0}", _bindAddresses);
            _logger.LogInformation("Using bind exclusions: {0}", _bindExclusions);
        }

        private void InitialiseRemote(NetworkConfiguration config)
        {
            RemoteAddressFilter = CreateIPCollection(config.RemoteIPFilter);
        }

        /// <summary>
        /// Initialises internal LAN cache settings.
        /// </summary>
        private void InitialiseLAN(NetworkConfiguration config)
        {
            lock (_intLock)
            {
                _logger.LogDebug("Refreshing LAN information.");

                // Get config options.
                string[] subnets = config.LocalNetworkSubnets;

                // Create lists from user settings.

                _lanSubnets = CreateIPCollection(subnets);
                _excludedSubnets = CreateIPCollection(subnets, true).AsNetworks();

                // If no LAN addresses are specified - all private subnets are deemed to be the LAN
                _usingPrivateAddresses = _lanSubnets.Count == 0;

                // NOTE: The order of the commands in this statement matters.
                if (_usingPrivateAddresses)
                {
                    _logger.LogDebug("Using LAN interface addresses as user provided no LAN details.");
                    // Internal interfaces must be private and not excluded.
                    _internalInterfaces = new NetCollection(_interfaceAddresses.Where(i => IsPrivateAddressRange(i) && !_excludedSubnets.Contains(i)));

                    // Subnets are the same as the calculated internal interface.
                    _lanSubnets = new NetCollection();

                    // We must listen on loopback for LiveTV to function regardless of the settings.
                    if (IsIP6Enabled)
                    {
                        _lanSubnets.Add(IPNetAddress.IP6Loopback);
                        _lanSubnets.Add(IPNetAddress.Parse("fc00::/7")); // ULA
                        _lanSubnets.Add(IPNetAddress.Parse("fe80::/10")); // Site local
                    }

                    if (IsIP4Enabled)
                    {
                        _lanSubnets.Add(IPNetAddress.IP4Loopback);
                        _lanSubnets.Add(IPNetAddress.Parse("10.0.0.0/8"));
                        _lanSubnets.Add(IPNetAddress.Parse("172.16.0.0/12"));
                        _lanSubnets.Add(IPNetAddress.Parse("192.168.0.0/16"));
                    }
                }
                else
                {
                    // We must listen on loopback for LiveTV to function regardless of the settings.
                    if (IsIP6Enabled)
                    {
                        _lanSubnets.Add(IPNetAddress.IP6Loopback);
                    }

                    if (IsIP4Enabled)
                    {
                        _lanSubnets.Add(IPNetAddress.IP4Loopback);
                    }

                    // Internal interfaces must be private, not excluded and part of the LocalNetworkSubnet.
                    _internalInterfaces = new NetCollection(_interfaceAddresses.Where(i => IsInLocalNetwork(i) && !_excludedSubnets.Contains(i) && _lanSubnets.Contains(i)));
                }

                _logger.LogInformation("Defined LAN addresses : {0}", _lanSubnets);
                _logger.LogInformation("Defined LAN exclusions : {0}", _excludedSubnets);
                _logger.LogInformation("Using LAN addresses: {0}", _lanSubnets.Exclude(_excludedSubnets).AsNetworks());
            }
        }

        /// <summary>
        /// Generate a list of all the interface ip addresses and submasks where that are in the active/unknown state.
        /// Generate a list of all active mac addresses that aren't loopback addresses.
        /// </summary>
        private void InitialiseInterfaces()
        {
            lock (_intLock)
            {
                _logger.LogDebug("Refreshing interfaces.");

                _interfaceNames.Clear();
                _interfaceAddresses.Clear();

                try
                {
                    IEnumerable<NetworkInterface> nics = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(i => i.SupportsMulticast && i.OperationalStatus == OperationalStatus.Up);

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
                                if (IsIP4Enabled && info.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    IPNetAddress nw = new IPNetAddress(info.Address, IPObject.MaskToCidr(info.IPv4Mask))
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv4Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    if ((ipProperties.GatewayAddresses.Count > 0) && !nw.IsLoopback())
                                    {
                                        // -ve Tags signify the interface has a gateway.
                                        nw.Tag *= -1;
                                    }

                                    _interfaceAddresses.Add(nw);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Description.ToLower(CultureInfo.InvariantCulture)] = tag;
                                    _interfaceNames["eth" + tag.ToString(CultureInfo.InvariantCulture)] = tag;
                                }
                                else if (IsIP6Enabled && info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    IPNetAddress nw = new IPNetAddress(info.Address, (byte)info.PrefixLength)
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv6Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    if ((ipProperties.GatewayAddresses.Count > 0) && !nw.IsLoopback())
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
                    _logger.LogDebug("Interfaces addresses : {0}", _interfaceAddresses);

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
                            _interfaceAddresses.Add(IPNetAddress.IP4Loopback);
                            if (IsIP6Enabled)
                            {
                                _interfaceAddresses.Add(IPNetAddress.IP6Loopback);
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

        /// <summary>
        /// Attempts to match the source against a user defined bind interface.
        /// </summary>
        /// <param name="source">IP source address to use.</param>
        /// <param name="isExternal">True if the source is in the external subnet.</param>
        /// <param name="bindPreference">The published server url that matches the source address.</param>
        /// <param name="port">The resultant port, if one exists.</param>
        /// <returns>True if a match is found.</returns>
        private bool MatchesPublishedServerUrl(IPObject source, bool isExternal, out string bindPreference, out int? port)
        {
            bindPreference = string.Empty;
            port = null;

            // Check for user override.
            foreach (var addr in _publishedServerUrls)
            {
                // Remaining. Match anything.
                if (addr.Key.Equals(IPAddress.Broadcast))
                {
                    bindPreference = addr.Value;
                    break;
                }
                else if ((addr.Key.Equals(IPAddress.Any) || addr.Key.Equals(IPAddress.IPv6Any)) && isExternal)
                {
                    // External.
                    bindPreference = addr.Value;
                    break;
                }
                else if (addr.Key.Contains(source))
                {
                    // Match ip address.
                    bindPreference = addr.Value;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(bindPreference))
            {
                // Has it got a port defined?
                var parts = bindPreference.Split(':');
                if (parts.Length > 1)
                {
                    if (int.TryParse(parts[1], out int p))
                    {
                        bindPreference = parts[0];
                        port = p;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to match the source against a user defined bind interface.
        /// </summary>
        /// <param name="source">IP source address to use.</param>
        /// <param name="isExternal">True if the source is in the external subnet.</param>
        /// <param name="result">The result, if a match is found.</param>
        /// <returns>True if a match is found.</returns>
        private bool MatchesBindInterface(IPObject source, bool isExternal, out string result)
        {
            result = string.Empty;
            var nc = _bindAddresses.Exclude(_bindExclusions);

            int count = nc.Count;
            if (count == 1 && (_bindAddresses[0].Equals(IPAddress.Any) || _bindAddresses[0].Equals(IPAddress.IPv6Any)))
            {
                // Ignore IPAny addresses.
                count = 0;
            }

            if (count != 0)
            {
                // Check to see if any of the bind interfaces are in the same subnet.

                NetCollection bindResult;
                IPAddress? defaultGateway = null;
                IPAddress? bindAddress;

                if (isExternal)
                {
                    // Find all external bind addresses. Store the default gateway, but check to see if there is a better match first.
                    bindResult = new NetCollection(nc
                        .Where(p => !IsInLocalNetwork(p))
                        .OrderBy(p => p.Tag));
                    defaultGateway = bindResult.FirstOrDefault()?.Address;
                    bindAddress = bindResult
                        .Where(p => p.Contains(source))
                        .OrderBy(p => p.Tag)
                        .FirstOrDefault()?.Address;
                }
                else
                {
                    // Look for the best internal address.
                    bindAddress = nc
                        .Where(p => IsInLocalNetwork(p) && (p.Contains(source) || p.Equals(IPAddress.None)))
                        .OrderBy(p => p.Tag)
                        .FirstOrDefault()?.Address;
                }

                if (bindAddress != null)
                {
                    result = FormatIP6String(bindAddress);
                    _logger.LogDebug("{0}: GetBindInterface: Has source, found a match bind interface subnets. {1}", source, result);
                    return true;
                }

                if (isExternal && defaultGateway != null)
                {
                    result = FormatIP6String(defaultGateway);
                    _logger.LogDebug("{0}: GetBindInterface: Using first user defined external interface. {1}", source, result);
                    return true;
                }

                result = FormatIP6String(nc.First().Address);
                _logger.LogDebug("{0}: GetBindInterface: Selected first user defined interface. {1}", source, result);

                if (isExternal)
                {
                    // TODO: remove this after testing.
                    _logger.LogWarning("{0}: External request received, however, only an internal interface bind found.", source);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to match the source against an external interface.
        /// </summary>
        /// <param name="source">IP source address to use.</param>
        /// <param name="result">The result, if a match is found.</param>
        /// <returns>True if a match is found.</returns>
        private bool MatchesExternalInterface(IPObject source, out string result)
        {
            result = string.Empty;
            // Get the first WAN interface address that isn't a loopback.
            var extResult = new NetCollection(_interfaceAddresses
                .Exclude(_bindExclusions)
                .Where(p => !IsInLocalNetwork(p))
                .OrderBy(p => p.Tag));

            if (extResult.Count > 0)
            {
                // Does the request originate in one of the interface subnets?
                // (For systems with multiple internal network cards, and multiple subnets)
                foreach (var intf in extResult)
                {
                    if (!IsInLocalNetwork(intf) && intf.Contains(source))
                    {
                        result = FormatIP6String(intf.Address);
                        _logger.LogDebug("{0}: GetBindInterface: Selected best external on interface on range. {1}", source, result);
                        return true;
                    }
                }

                result = FormatIP6String(extResult.First().Address);
                _logger.LogDebug("{0}: GetBindInterface: Selected first external interface. {0}", source, result);
                return true;
            }

            // Have to return something, so return an internal address

            // TODO: remove this after testing.
            _logger.LogWarning("{0}: External request received, however, no WAN interface found.", source);
            return false;
        }
    }
}
