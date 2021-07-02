using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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

namespace Jellyfin.Networking.Manager
{
    /// <summary>
    /// Class to take care of network interface management.
    /// Note: The normal collection methods and properties will not work with Collection{IPNetAddress}. <see cref="MediaBrowser.Common.Net.NetworkExtensions"/>.
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
        private readonly object _intLock;

        /// <summary>
        /// List of all interface addresses and masks.
        /// </summary>
        private readonly Collection<IPNetAddress> _interfaceAddresses;

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

        private IpClassType _ipClassType;

        private IPNetAddress[] _remoteAddressFilter;

        /// <summary>
        /// Used to stop "event-racing conditions".
        /// </summary>
        private bool _eventfire;

        /// <summary>
        /// Unfiltered user defined LAN subnets. (<see cref="NetworkConfiguration.LocalNetworkSubnets"/>)
        /// or internal interface network subnets if undefined by user.
        /// </summary>
        private Collection<IPNetAddress> _lanSubnets;

        /// <summary>
        /// User defined list of subnets to excluded from the LAN.
        /// </summary>
        private Collection<IPNetAddress> _excludedSubnets;

        /// <summary>
        /// List of interface addresses to bind the WS.
        /// </summary>
        private IPNetAddress[] _bindAddresses;

        /// <summary>
        /// List of interface addresses to exclude from bind.
        /// </summary>
        private IPNetAddress[] _bindExclusions;

        /// <summary>
        /// Caches list of all internal filtered interface addresses and masks.
        /// </summary>
        private Collection<IPNetAddress> _internalInterfaces;

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
            _intLock = new ();
            _ipClassType = IpClassType.IpBoth;
            _interfaceAddresses = new Collection<IPNetAddress>();
            _macAddresses = new List<PhysicalAddress>();
            _interfaceNames = new Dictionary<string, int>();
            _publishedServerUrls = new Dictionary<IPNetAddress, string>();
            _eventFireLock = new object();
            _remoteAddressFilter = Array.Empty<IPNetAddress>();

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
        /// Gets a value indicating the current IP status of the system.
        /// </summary>
        public IpClassType IpClassType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        public bool IsIP6Enabled => _ipClassType != IpClassType.Ip4Only;

        /// <summary>
        /// Gets a value indicating whether IP4 is enabled.
        /// </summary>
        public bool IsIP4Enabled => _ipClassType != IpClassType.Ip6Only;

        /// <summary>
        /// Gets a value indicating whether is all IPv6 interfaces are trusted as internal.
        /// </summary>
        public bool TrustAllIP6Interfaces { get; private set; }

        /// <summary>
        /// Gets the Published server override list.
        /// </summary>
        public Dictionary<IPNetAddress, string> PublishedServerUrls => _publishedServerUrls;

        /// <summary>
        /// Creates a new network collection.
        /// </summary>
        /// <param name="source">Items to assign the collection, or null.</param>
        /// <returns>The collection created.</returns>
        public static Collection<IPNetAddress> CreateCollection(IEnumerable<IPNetAddress>? source = null)
        {
            var result = new Collection<IPNetAddress>();
            if (source != null)
            {
                foreach (var item in source)
                {
                    result.AddItem(item, false);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the IPAddress contains an IP6 Local link address.
        /// </summary>
        /// <param name="address">IPAddress object to check.</param>
        /// <returns>True if it is a local link address.</returns>
        /// <remarks>
        /// See https://stackoverflow.com/questions/6459928/explain-the-instance-properties-of-system-net-ipaddress
        /// it appears that the IPAddress.IsIPv6LinkLocal is out of date.
        /// </remarks>
        public static bool IsIPv6LinkLocal(IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return false;
            }

            // GetAddressBytes
            Span<byte> octet = stackalloc byte[16];
            address.TryWriteBytes(octet, out _);
            uint word = (uint)(octet[0] << 8) + octet[1];

            return word >= 0xfe80 && word <= 0xfebf; // fe80::/10 :Local link.
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<PhysicalAddress> GetMacAddresses()
        {
            // Populated in construction - so always has values.
            return _macAddresses;
        }

        /// <inheritdoc/>
        public bool IsExcluded(EndPoint ip)
        {
            return _excludedSubnets.ContainsAddress(((IPEndPoint)ip).Address);
        }

        /// <inheritdoc/>
        public IPNetAddress[] GetLoopbacks()
        {
            if (_ipClassType == IpClassType.IpBoth)
            {
                return new[] { IPNetAddress.IP4Loopback, IPNetAddress.IP6Loopback };
            }

            if (_ipClassType == IpClassType.Ip6Only)
            {
                return new[] { IPNetAddress.IP6Loopback };
            }

            return new[] { IPNetAddress.IP4Loopback };
        }

        /// <inheritdoc/>
        public Collection<IPNetAddress> CreateIPCollection(string[] values, bool negated, bool combineNetworks)
        {
            Collection<IPNetAddress> col = new ();

            foreach (var value in values)
            {
                string v = value.Trim();

                try
                {
                    if (v.StartsWith('!'))
                    {
                        if (negated)
                        {
                            AddToCollection(col, v[1..], combineNetworks);
                        }
                    }
                    else if (!negated)
                    {
                        AddToCollection(col, v, combineNetworks);
                    }
                }
                catch (ArgumentException e)
                {
                    _logger.LogWarning(e, "Ignoring LAN value {Value}.", v);
                }
            }

            return col;
        }

        /// <inheritdoc/>
        public IPNetAddress[] GetAllBindInterfaces(bool individualInterfaces = false)
        {
            if (_bindAddresses.Length == 0)
            {
                if (_bindExclusions.Length > 0)
                {
                    // Return all the interfaces except the ones specifically excluded.
                    return _interfaceAddresses.Exclude(_bindExclusions, false).ToArray();
                }

                // No bind address and no exclusions, so listen on all interfaces.
                var result = new Collection<IPNetAddress>();

                if (individualInterfaces)
                {
                    foreach (var iface in _interfaceAddresses)
                    {
                        result.AddItem(iface, true);
                    }

                    return result.ToArray();
                }

                if (IpClassType == IpClassType.IpBoth)
                {
                    // Kestrel source code shows it uses Sockets.DualMode - so this also covers IPAddress.Any
                    result.AddItem(new IPNetAddress(IPAddress.IPv6Any, 128), true);
                }
                else if (IpClassType == IpClassType.Ip4Only)
                {
                    result.AddItem(new IPNetAddress(IPAddress.Any, 32), true);
                }
                else if (IpClassType == IpClassType.Ip6Only)
                {
                    // Cannot use IPv6Any as Kestrel will bind to IPv4 addresses.
                    foreach (var iface in _interfaceAddresses)
                    {
                        if (iface.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            result.AddItem(new IPNetAddress(iface.Address, 128), true);
                        }
                    }
                }

                return result.ToArray();
            }

            // Remove any excluded bind interfaces.
            return _bindAddresses.Exclude(_bindExclusions, false).ToArray();
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

            if (IPHost.TryParse(source.Host.Host, out IPHost? host, _ipClassType))
            {
                result = GetBindInterface(host, out port);
                port ??= source.Host.Port;
            }
            else
            {
                result = string.Empty;
                port = null;
            }

            return result;
        }

        /// <inheritdoc/>
        public string GetBindInterface(IPNetAddress? source, out int? port)
        {
            port = null;

            string result;

            if (source != null)
            {
                if (IpClassType != IpClassType.Ip6Only && source.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _logger.LogWarning("IPv6 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
                }

                if (IpClassType != IpClassType.Ip4Only && source.AddressFamily == AddressFamily.InterNetwork)
                {
                    _logger.LogWarning("IPv4 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
                }

                bool isExternal = !IsInLocalNetwork(source);
                _logger.LogDebug("GetBindInterface with source. External: {IsExternal}:", isExternal);

                if (MatchesPublishedServerUrl(source, isExternal, out string res, out port))
                {
                    _logger.LogInformation("{Source}: Using BindAddress {Address}:{Port}", source, res, port);
                    return res;
                }

                // No preference given, so move on to bind addresses.
                if (MatchesBindInterface(source, isExternal, out result))
                {
                    return result;
                }

                if (isExternal && MatchesExternalInterface(source, out result))
                {
                    return result;
                }
            }
            else if (_bindAddresses.Length > 0)
            {
                return _bindAddresses[0].Address.ToString();
            }

            // Get the first LAN interface address that isn't a loopback.
            var interfaces = CreateCollection(_interfaceAddresses
                .Exclude(_bindAddresses, false)
                .Where(IsInLocalNetwork)
                .OrderBy(p => p.Tag));

            if (interfaces.Count > 0)
            {
                if (source != null)
                {
                    foreach (var intf in interfaces)
                    {
                        if (intf.Address.Equals(source.Address))
                        {
                            result = FormatIp6String(intf.Address);
                            _logger.LogDebug("{Source}: GetBindInterface: Has found matching interface. {Result}", source, result);
                            return result;
                        }
                    }

                    // Does the request originate in one of the interface subnets?
                    // (For systems with multiple internal network cards, and multiple subnets)
                    foreach (var intf in interfaces)
                    {
                        if (intf.Contains(source))
                        {
                            result = FormatIp6String(intf.Address);
                            _logger.LogDebug("{Source}: GetBindInterface: Has source, matched best internal interface on range. {Result}", source, result);
                            return result;
                        }
                    }
                }

                result = FormatIp6String(interfaces.First().Address);
                _logger.LogDebug("{Source}: GetBindInterface: Matched first internal interface. {Result}", source, result);
                return result;
            }

            // There isn't any others, so we'll use the loopback.
            result = IpClassType == IpClassType.Ip4Only ? "127.0.0.1" : "::1";
            _logger.LogWarning("{Source}: GetBindInterface: Loopback {Result} returned.", source, result);
            return result;
        }

        /// <inheritdoc/>
        public IPNetAddress[] GetInternalBindAddresses()
        {
            if (_bindAddresses.Length == 0)
            {
                if (_bindExclusions.Length > 0)
                {
                    // Return all the internal interfaces except the ones excluded.
                    return _internalInterfaces.Where(p => !_bindExclusions.ContainsAddress(p)).ToArray();
                }

                // No bind address, so return all internal interfaces.
                return _internalInterfaces.ToArray();
            }

            return _bindAddresses.Where(p => IsInLocalNetwork(p)).ToArray();
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(IPNetAddress address)
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
            return address.IsLoopback() || (_lanSubnets.ContainsAddress(address) && !_excludedSubnets.ContainsAddress(address));
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(string address)
        {
            if (IPNetAddress.TryParse(address, out var ep, _ipClassType) || IPHost.TryParse(address, out ep, _ipClassType))
            {
                return ep.IsLoopback() || (_lanSubnets.ContainsAddress(ep) && !_excludedSubnets.ContainsAddress(ep));
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

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            // As private addresses can be redefined by Configuration.LocalNetworkAddresses
            return address.Equals(IPAddress.Loopback) ||
                address.Equals(IPAddress.IPv6Loopback) ||
                (_lanSubnets.ContainsAddress(address) && !_excludedSubnets.ContainsAddress(address));
        }

        private bool IsPrivateAddressRange(IPNetAddress address)
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
        public bool TryParseInterface(string token, [NotNullWhen(true)] out Collection<IPNetAddress>? result)
        {
            result = null;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            if (_interfaceNames.TryGetValue(token.ToLower(CultureInfo.InvariantCulture), out int index))
            {
                result = new Collection<IPNetAddress>();

                _logger.LogInformation("Interface {Token} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (Math.Abs(iface.Tag) == index &&
                        ((IpClassType != IpClassType.Ip6Only && iface.Address.AddressFamily == AddressFamily.InterNetwork) ||
                         (IpClassType != IpClassType.Ip4Only && iface.Address.AddressFamily == AddressFamily.InterNetworkV6)))
                    {
                        result.AddItem(iface, false);
                    }
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool HasRemoteAccess(IPAddress remoteIp)
        {
            var config = _configurationManager.GetNetworkConfiguration();
            if (config.EnableRemoteAccess)
            {
                // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
                // If left blank, all remote addresses will be allowed.
                if (_remoteAddressFilter.Length > 0 && !IsInLocalNetwork(remoteIp))
                {
                    // remoteAddressFilter is a whitelist or blacklist.
                    return _remoteAddressFilter.ContainsAddress(remoteIp) == !config.IsRemoteIPFilterBlacklist;
                }
            }
            else if (!IsInLocalNetwork(remoteIp))
            {
                // Remote not enabled. So everyone should be LAN.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reloads all settings and re-initialises the instance.
        /// </summary>
        /// <param name="configuration">The <see cref="NetworkConfiguration"/> to use.</param>
        public void UpdateSettings(object configuration)
        {
            NetworkConfiguration config = (NetworkConfiguration)configuration ?? throw new ArgumentNullException(nameof(configuration));

            _ipClassType = config.ClassType();

            if (!Socket.OSSupportsIPv4 && _ipClassType != IpClassType.Ip4Only)
            {
                // IP4 not available - so use only IP6.
                _ipClassType = IpClassType.Ip6Only;
            }

            if (!Socket.OSSupportsIPv6 && _ipClassType != IpClassType.Ip6Only)
            {
                // IP6 not available - so use only IP4
                _ipClassType = IpClassType.Ip4Only;
            }

            TrustAllIP6Interfaces = config.TrustAllIP6Interfaces;

            if (string.IsNullOrEmpty(MockNetworkSettings))
            {
                InitialiseInterfaces();
            }
            else // Used in testing only.
            {
                // Format is <IPAddress>,<Index>,<Name>: <next interface>. Set index to -ve to simulate a gateway.
                var interfaceList = MockNetworkSettings.Split('|');
                foreach (var details in interfaceList)
                {
                    var parts = details.Split(',');
                    var address = IPNetAddress.Parse(parts[0]);
                    var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    address.Tag = index;
                    _interfaceAddresses.AddItem(address, false);
                    _interfaceNames[parts[2]] = Math.Abs(index);
                }

                // if (IpClassType == IpClassType.Ip4Only)
                // {
                //     _interfaceAddresses.AddItem(IPNetAddress.IP4Loopback, false);
                // }

                // if (IpClassType == IpClassType.Ip6Only)
                // {
                //     _interfaceAddresses.AddItem(IPNetAddress.IP6Loopback, false);
                // }
            }

            InitialiseLan(config);
            InitialiseBind(config);
            InitialiseRemote(config);
            InitialiseOverrides(config);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"><c>True</c> to dispose the managed state.</param>
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
        /// Tries to identify the string and return an object of that class.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <param name="result">IPNetAddress to return.</param>
        /// <returns><c>true</c> if the value parsed successfully, <c>false</c> otherwise.</returns>
        private bool TryParse(string addr, [NotNullWhen(true)] out IPNetAddress? result)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                // Is it an IP address
                if (IPNetAddress.TryParse(addr, out IPNetAddress? nw, _ipClassType))
                {
                    result = nw;
                    return true;
                }

                if (IPHost.TryParse(addr, out IPHost? h, _ipClassType))
                {
                    result = h;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void ConfigurationUpdated(object? sender, ConfigurationUpdateEventArgs evt)
        {
            if (evt.Key.Equals("network", StringComparison.Ordinal))
            {
                UpdateSettings((NetworkConfiguration)evt.NewConfiguration);
            }
        }

        /// <summary>
        /// Checks the string to see if it matches any interface names.
        /// </summary>
        /// <param name="token">String to check.</param>
        /// <param name="index">Interface index numbers that match.</param>
        /// <returns><c>true</c> if an interface name matches the token, <c>False</c> otherwise.</returns>
        private bool TryGetInterfaces(string token, [NotNullWhen(true)] out List<int>? index)
        {
            index = null;

            // Is it the name of an interface (windows) eg, Wireless LAN adapter Wireless Network Connection 1.
            // Null check required here for automated testing.
            if (token.Length > 1)
            {
                bool partial = token[^1] == '*';
                if (partial)
                {
                    token = token[..^1];
                }

                foreach ((string interfc, int interfcIndex) in _interfaceNames)
                {
                    if ((!partial && string.Equals(interfc, token, StringComparison.OrdinalIgnoreCase))
                        || (partial && interfc.StartsWith(token, StringComparison.OrdinalIgnoreCase)))
                    {
                        index ??= new List<int>();
                        index.Add(interfcIndex);
                    }
                }
            }

            return index != null;
        }

        /// <summary>
        /// Converts an IPAddress into a string.
        /// Ipv6 addresses are returned in [ ], with their scope removed.
        /// </summary>
        /// <param name="address">Address to convert.</param>
        /// <returns>URI safe conversion of the address.</returns>
        private static string FormatIp6String(IPAddress? address)
        {
            if (address == null)
            {
                return string.Empty;
            }

            var str = address.ToString();
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                int i = str.IndexOf('%', StringComparison.Ordinal);
                if (i != -1)
                {
                    str = str.Substring(0, i);
                }

                return $"[{str}]";
            }

            return str;
        }

        /// <summary>
        /// Parses a string and adds it into the collection, replacing any interface references.
        /// </summary>
        /// <param name="col"><see cref="Collection{IPNetAddress}"/>Collection.</param>
        /// <param name="token">String value to parse.</param>
        /// <param name="itemIsNetwork">True if the item is a network.</param>
        private void AddToCollection(Collection<IPNetAddress> col, string token, bool itemIsNetwork)
        {
            // Is it the name of an interface (windows) eg, Wireless LAN adapter Wireless Network Connection 1.
            // Null check required here for automated testing.
            if (TryGetInterfaces(token, out var indices))
            {
                _logger.LogInformation("Interface {Token} used in settings. Using its interface addresses.", token);

                // Replace all the interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (indices.Contains(Math.Abs(iface.Tag))
                        && ((IpClassType != IpClassType.Ip6Only && iface.Address.AddressFamily == AddressFamily.InterNetwork)
                            || (IpClassType != IpClassType.Ip4Only && iface.Address.AddressFamily == AddressFamily.InterNetworkV6)))
                    {
                        col.AddItem(iface, itemIsNetwork);
                    }
                }
            }
            else if (TryParse(token, out var obj))
            {
                col.AddItem(obj, itemIsNetwork);
            }
            else
            {
                _logger.LogDebug("Invalid or unknown object {Token}.", token);
            }
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">A <see cref="NetworkAvailabilityEventArgs"/> containing network availability information.</param>
        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            _logger.LogDebug("Network availability changed.");
            OnNetworkChanged();
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">An <see cref="EventArgs"/>.</param>
        private void OnNetworkAddressChanged(object? sender, EventArgs e)
        {
            _logger.LogDebug("Network address change detected.");
            OnNetworkChanged();
        }

        /// <summary>
        /// Async task that waits for 2 seconds before re-initialising the settings, as typically these events fire multiple times in succession.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task OnNetworkChangeAsync()
        {
            try
            {
                await Task.Delay(2000).ConfigureAwait(false);
                InitialiseInterfaces();
                // Recalculate LAN caches.
                InitialiseLan(_configurationManager.GetNetworkConfiguration());

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

                foreach (var entry in overrides)
                {
                    var parts = entry.Split('=');
                    if (parts.Length != 2)
                    {
                        _logger.LogError("Unable to parse bind override: {Entry}", entry);
                    }
                    else
                    {
                        var replacement = parts[1].Trim();
                        if (string.Equals(parts[0], "all", StringComparison.OrdinalIgnoreCase))
                        {
                            _publishedServerUrls[new IPNetAddress(IPAddress.Broadcast)] = replacement;
                        }
                        else if (string.Equals(parts[0], "external", StringComparison.OrdinalIgnoreCase))
                        {
                            _publishedServerUrls[new IPNetAddress(IPAddress.Any)] = replacement;
                        }
                        else if (TryParseInterface(parts[0], out Collection<IPNetAddress>? addresses))
                        {
                            foreach (IPNetAddress na in addresses)
                            {
                                _publishedServerUrls[na] = replacement;
                            }
                        }
                        else if (IPNetAddress.TryParse(parts[0], out IPNetAddress? result, _ipClassType))
                        {
                            _publishedServerUrls[result] = replacement;
                        }
                        else
                        {
                            _logger.LogError("Unable to parse bind ip address. {Parts}", parts[1]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialises the network bind addresses.
        /// </summary>
        private void InitialiseBind(NetworkConfiguration config)
        {
            lock (_intLock)
            {
                string[] intAddresses = config.LocalNetworkAddresses;

                // Add virtual machine interface names to the list of bind exclusions, so that they are auto-excluded.
                if (config.IgnoreVirtualInterfaces)
                {
                    // each virtual interface name must be pre-pended with the exclusion symbol !
                    var virtualInterfaceNames = config.VirtualInterfaceNames.Split(',').Select(p => '!' + p).ToArray();
                    if (intAddresses.Length > 0)
                    {
                        var newList = new string[intAddresses.Length + virtualInterfaceNames.Length];
                        Array.Copy(intAddresses, newList, intAddresses.Length);
                        Array.Copy(virtualInterfaceNames, 0, newList, intAddresses.Length, virtualInterfaceNames.Length);
                        intAddresses = newList;
                    }
                    else
                    {
                        intAddresses = virtualInterfaceNames;
                    }
                }

                // Read and parse bind addresses and exclusions, removing ones that don't exist.
                _bindExclusions = CreateIPCollection(intAddresses, true, false)
                    .Where(p => _interfaceAddresses.ContainsAddress(p))
                    .ToArray();

                _bindAddresses = CreateIPCollection(intAddresses, false, false)
                    .Where(p => _interfaceAddresses.ContainsAddress(p))
                    .ToArray();

                _logger.LogInformation("Using bind addresses: {0}", _bindAddresses.AsString());
                _logger.LogInformation("Using bind exclusions: {0}", _bindExclusions.AsString());
            }
        }

        /// <summary>
        /// Initialises the remote address values.
        /// </summary>
        private void InitialiseRemote(NetworkConfiguration config)
        {
            lock (_intLock)
            {
                _remoteAddressFilter = CreateIPCollection(config.RemoteIPFilter, false, false).ToArray();
            }
        }

        /// <summary>
        /// Initialises internal LAN cache settings.
        /// </summary>
        private void InitialiseLan(NetworkConfiguration config)
        {
            lock (_intLock)
            {
                _logger.LogDebug("Refreshing LAN information.");

                // Get configuration options.
                string[] subnets = config.LocalNetworkSubnets;
                // Create lists from user settings.

                _lanSubnets = CreateIPCollection(subnets, false, true);
                _excludedSubnets = CreateIPCollection(subnets, true, true).AsNetworkAddresses();

                // NOTE: The order of the commands generating the collection in this statement matters.
                // Altering the order will cause the collections to be created incorrectly.
                if (_lanSubnets.Count == 0)
                {
                    // If no LAN addresses are specified - all private subnets are deemed to be the LAN.
                    _logger.LogDebug("Using LAN interface addresses as user provided no LAN details.");
                    // Internal interfaces must be private and not excluded.
                    _internalInterfaces = CreateCollection(
                        _interfaceAddresses.Where(i => IsPrivateAddressRange(i) && !_excludedSubnets.ContainsAddress(i)));

                    if (IpClassType == IpClassType.Ip6Only)
                    {
                        _lanSubnets.AddItem(IPNetAddress.Parse("fc00::/7"), true); // ULA
                        _lanSubnets.AddItem(IPNetAddress.Parse("fe80::/10"), true); // Site local
                    }

                    if (IpClassType == IpClassType.Ip4Only)
                    {
                        _lanSubnets.AddItem(IPNetAddress.Parse("10.0.0.0/8"), true);
                        _lanSubnets.AddItem(IPNetAddress.Parse("172.16.0.0/12"), true);
                        _lanSubnets.AddItem(IPNetAddress.Parse("192.168.0.0/16"), true);
                    }
                }
                else
                {
                    // Internal interfaces must be private, not excluded and part of the LocalNetworkSubnet.
                    _internalInterfaces = CreateCollection(_interfaceAddresses.Where(IsInLocalNetwork));
                }

                _logger.LogInformation("Defined LAN addresses : {0}", _lanSubnets.AsString());
                _logger.LogInformation("Defined LAN exclusions : {0}", _excludedSubnets.AsString());
                _logger.LogInformation("Using LAN addresses: {0}", _lanSubnets.Exclude(_excludedSubnets, true).AsNetworkAddresses().AsString());
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
                _macAddresses.Clear();

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
                            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && PhysicalAddress.None.Equals(mac))
                            {
                                _macAddresses.Add(mac);
                            }

                            // populate interface address list
                            foreach (UnicastIPAddressInformation info in ipProperties.UnicastAddresses)
                            {
                                if (IpClassType != IpClassType.Ip6Only && info.Address.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    IPNetAddress nw = new (info.Address, IPNetAddress.MaskToCidr(info.IPv4Mask))
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv4Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    if (ipProperties.GatewayAddresses.Count > 0 && !nw.IsLoopback())
                                    {
                                        // -ve Tags signify the interface has a gateway.
                                        nw.Tag *= -1;
                                    }

                                    _interfaceAddresses.AddItem(nw, false);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Description.ToLower(CultureInfo.InvariantCulture)] = tag;
                                    _interfaceNames["eth" + tag.ToString(CultureInfo.InvariantCulture)] = tag;
                                }
                                else if (IpClassType != IpClassType.Ip4Only && info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    IPNetAddress nw = new (info.Address, (byte)info.PrefixLength)
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv6Properties().Index
                                    };

                                    int tag = nw.Tag;
                                    if (ipProperties.GatewayAddresses.Count > 0 && !nw.IsLoopback())
                                    {
                                        // -ve Tags signify the interface has a gateway.
                                        nw.Tag *= -1;
                                    }

                                    _interfaceAddresses.AddItem(nw, false);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Description.ToLower(CultureInfo.InvariantCulture)] = tag;
                                    _interfaceNames["eth" + tag.ToString(CultureInfo.InvariantCulture)] = tag;
                                }
                            }
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                        {
                            // Ignore error, and attempt to continue.
                            _logger.LogError(ex, "Error encountered parsing interfaces.");
                        }
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogError(ex, "Error obtaining interfaces.");
                }

                // If for some reason we don't have an interface info, resolve our DNS name.
                if (_interfaceAddresses.Count == 0)
                {
                    _logger.LogError("No interfaces information available. Resolving DNS name.");
                    IPHost host = new (Dns.GetHostName());
                    foreach (IPAddress a in host.GetAddresses())
                    {
                        _interfaceAddresses.AddItem(new IPNetAddress(a), false);
                    }

                    if (_interfaceAddresses.Count == 0)
                    {
                        _logger.LogWarning("No interfaces information available. Using loopback.");
                    }
                }

                if (IpClassType == IpClassType.Ip4Only && !_interfaceAddresses.ContainsAddress(IPNetAddress.IP4Loopback))
                {
                    _interfaceAddresses.AddItem(IPNetAddress.IP4Loopback, false);
                }

                if (IpClassType == IpClassType.Ip6Only && !_interfaceAddresses.ContainsAddress(IPNetAddress.IP6Loopback))
                {
                    _interfaceAddresses.AddItem(IPNetAddress.IP6Loopback, false);
                }

                _logger.LogDebug("Discovered {0} interfaces.", _interfaceAddresses.Count);
                _logger.LogDebug("Interfaces addresses : {0}", _interfaceAddresses.AsString());
            }
        }

        /// <summary>
        /// Attempts to match the source against a user defined bind interface.
        /// </summary>
        /// <param name="source">IP source address to use.</param>
        /// <param name="isInExternalSubnet">True if the source is in the external subnet.</param>
        /// <param name="bindPreference">The published server url that matches the source address.</param>
        /// <param name="port">The resultant port, if one exists.</param>
        /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
        private bool MatchesPublishedServerUrl(IPNetAddress source, bool isInExternalSubnet, out string bindPreference, out int? port)
        {
            bindPreference = string.Empty;
            port = null;

            // Check for user override.
            foreach (var addr in _publishedServerUrls)
            {
                // Remaining. Match anything.
                if (addr.Key.Address.Equals(IPAddress.Broadcast))
                {
                    bindPreference = addr.Value;
                    break;
                }
                else if ((addr.Key.Address.Equals(IPAddress.Any) || addr.Key.Address.Equals(IPAddress.IPv6Any)) && isInExternalSubnet)
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

            if (string.IsNullOrEmpty(bindPreference))
            {
                return false;
            }

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

        /// <summary>
        /// Attempts to match the source against a user defined bind interface.
        /// </summary>
        /// <param name="source">IP source address to use.</param>
        /// <param name="isInExternalSubnet">True if the source is in the external subnet.</param>
        /// <param name="result">The result, if a match is found.</param>
        /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
        private bool MatchesBindInterface(IPNetAddress source, bool isInExternalSubnet, out string result)
        {
            result = string.Empty;
            var addresses = _bindAddresses;

            int count = addresses.Length;
            if (count == 1 && (_bindAddresses[0].Equals(IPAddress.Any) || _bindAddresses[0].Equals(IPAddress.IPv6Any)))
            {
                // Ignore IPAny addresses.
                count = 0;
            }

            if (count != 0)
            {
                // Check to see if any of the bind interfaces are in the same subnet.

                IPAddress? defaultGateway = null;
                IPAddress? bindAddress = null;

                if (isInExternalSubnet)
                {
                    // Find all external bind addresses. Store the default gateway, but check to see if there is a better match first.
                    foreach (var addr in addresses.OrderBy(p => p.Tag))
                    {
                        if (defaultGateway == null && !IsInLocalNetwork(addr))
                        {
                            defaultGateway = addr.Address;
                        }

                        if (bindAddress == null && addr.Contains(source))
                        {
                            bindAddress = addr.Address;
                        }

                        if (defaultGateway != null && bindAddress != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // Look for the best internal address.
                    bindAddress = addresses
                        .Where(p => IsInLocalNetwork(p) && (p.Contains(source) || p.Equals(IPAddress.Any)))
                        .OrderBy(p => p.Tag)
                        .FirstOrDefault()?.Address;
                }

                if (bindAddress != null)
                {
                    result = FormatIp6String(bindAddress);
                    _logger.LogDebug("{Source}: GetBindInterface: Has source, found a match bind interface subnets. {Result}", source, result);
                    return true;
                }

                if (isInExternalSubnet && defaultGateway != null)
                {
                    result = FormatIp6String(defaultGateway);
                    _logger.LogDebug("{Source}: GetBindInterface: Using first user defined external interface. {Result}", source, result);
                    return true;
                }

                result = FormatIp6String(addresses[0].Address);
                _logger.LogDebug("{Source}: GetBindInterface: Selected first user defined interface. {Result}", source, result);

                if (isInExternalSubnet)
                {
                    _logger.LogWarning("{Source}: External request received, however, only an internal interface bind found.", source);
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
        /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
        private bool MatchesExternalInterface(IPNetAddress source, out string result)
        {
            result = string.Empty;
            // Get the first WAN interface address that isn't a loopback.
            var extResult = _interfaceAddresses
                .Where(p => !IsInLocalNetwork(p))
                .OrderBy(p => p.Tag);

            IPAddress? hasResult = null;
            // Does the request originate in one of the interface subnets?
            // (For systems with multiple internal network cards, and multiple subnets)
            foreach (var intf in extResult)
            {
                hasResult ??= intf.Address;
                if (!IsInLocalNetwork(intf) && intf.Contains(source))
                {
                    result = FormatIp6String(intf.Address);
                    _logger.LogDebug("{Source}: GetBindInterface: Selected best external on interface on range. {Result}", source, result);
                    return true;
                }
            }

            if (hasResult != null)
            {
                result = FormatIp6String(hasResult);
                _logger.LogDebug("{Source}: GetBindInterface: Selected first external interface. {Result}", source, result);
                return true;
            }

            _logger.LogDebug("{Source}: External request received, but no WAN interface found. Need to route through internal network.", source);
            return false;
        }
    }
}
