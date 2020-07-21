#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Networking;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Networking
{
    /// <summary>
    /// Class to take care of network interface management.
    /// </summary>
    public class NetworkManager : INetworkManager
    {
        /// <summary>
        /// Gets the singleton of this object.
        /// </summary>
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable SA1401 // Fields should be private
        public static NetworkManager Instance = null!;
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore CA2211 // Non-constant fields should not be visible

        /// <summary>
        /// Defines the _interfaceNames.
        /// </summary>
        private readonly SortedList<string, int> _interfaceNames;

        /// <summary>
        /// Threading object for network interfaces.
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

        private ILogger? _logger;

        /// <summary>
        /// Used to stop "event-racing conditions".
        /// </summary>
        private bool _eventfire;

        /// <summary>
        /// True if IP6 addresses be ignored.
        /// </summary>
        private bool _ignoreIP6 = true;

        /// <summary>
        /// Unfiltered user defined LAN addresses,
        /// or internal interface network addresses if undefined by user.
        /// </summary>
        private NetCollection _lanAddresses;

        /// <summary>
        /// User defined list of addresses to excluded from the LAN.
        /// </summary>
        private NetCollection _excludedAddresses;

        /// <summary>
        /// Cached list of filtered addresses comprising the LAN.
        /// (_lanAddresses ?? _interfaceAddresses).Exclude(_excludedAddresses).
        /// </summary>
        private NetCollection _filteredLANAddresses;

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
        private NetCollection _internalInterfaceAddresses;

        /// <summary>
        /// Flag set when _lanAddressses is set to _interfaceAddresses as no custom LAN has been defined in the config.
        /// </summary>
        private bool _usingInterfaces;

        /// <summary>
        /// Function that return the LAN addresses from the config.
        /// </summary>
        private Func<string[]> _localSubnetsFn;

        /// <summary>
        /// Function that return the IP  addresses from the config.
        /// </summary>
        private Func<string[]> _bindAddressesFn;

        /// <summary>
        /// Gets or sets the EnableIPV6 setting from config.
        /// </summary>
        private Func<bool> _isIP6EnabledFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        /// <param name="logger">Logger to use for messages.</param>
        /// <param name="ip6Enabled">Function that returns the EnableIPV6 config option.</param>
        /// <param name="subnets">Function that returns the LocalNetworkSubnets config option.</param>
        /// <param name="bindInterfaces">Function that returns the LocalNetworkAddresses config option.</param>
        public NetworkManager(ILogger<NetworkManager>? logger, Func<bool> ip6Enabled, Func<string[]> subnets, Func<string[]> bindInterfaces)
        {
            _logger = logger;

            _interfaceAddresses = new NetCollection();
            _macAddresses = new List<PhysicalAddress>();
            _interfaceNames = new SortedList<string, int>();

            // Assign empty objects to the rest of the properties
            // so we don't have to define them as nullable.

            _isIP6EnabledFn = ip6Enabled ?? throw new ArgumentNullException(nameof(ip6Enabled));
            _ignoreIP6 = !_isIP6EnabledFn();

            InitialiseInterfaces();

            _localSubnetsFn = subnets;
            InitialiseLAN();

            _bindAddressesFn = bindInterfaces ?? throw new ArgumentNullException(nameof(bindInterfaces));
            InitialiseBind();

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

            Instance = this;
        }

        /// <summary>
        /// Event triggered on network changes.
        /// </summary>
        public event EventHandler? NetworkChanged;

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        public bool IsIP6Enabled
        {
            get
            {
                return !_ignoreIP6;
            }
        }

        /// <summary>
        /// Returns true if the IP address in address2 is within the network address1/subnetMask.
        /// </summary>
        /// <param name="subnetIP">Subnet IP.</param>
        /// <param name="subnetMask">Subnet Mask.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if address is in the subnet.</returns>
        public bool IsInSameSubnet(IPAddress subnetIP, IPAddress subnetMask, IPAddress address)
        {
            return IPObject.NetworkAddress(subnetIP, subnetMask) == IPObject.NetworkAddress(address, subnetMask);
        }

        /// <summary>
        /// Event triggered when configuration is changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">New configuration.</param>
        public void ConfigurationUpdated(object sender, EventArgs e)
        {
            // IP6 settings changed.
            if (_ignoreIP6 == _isIP6EnabledFn())
            {
                InitialiseInterfaces();
                _ignoreIP6 = !_ignoreIP6;
            }

            InitialiseLAN();
            InitialiseBind();
        }

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetRandomUnusedUdpPort()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            #pragma warning disable IDE0063 // Use simple 'using' statement - want the item to be disposed of immediately.
            using (var udpClient = new UdpClient(localEndPoint))
            #pragma warning restore IDE0063
            {
                return ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
            }
        }

        /// <summary>
        /// Get a list of all the MAC addresses associated with active interfaces.
        /// </summary>
        /// <returns>List of MAC addresses.</returns>
        public List<PhysicalAddress> GetMacAddresses()
        {
            // Populated in construction - so always has values.
            lock (_intLock)
            {
                return _macAddresses.ToList();
            }
        }

        /// <summary>
        /// Returns true if the IP address is in the excluded list.
        /// </summary>
        /// <param name="ip">IP to check.</param>
        /// <returns>True if excluded.</returns>
        public bool IsExcluded(IPAddress ip)
        {
            return _excludedAddresses.Exists(ip);
        }

        /// <summary>
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        public bool IsInLocalNetwork(string endpoint)
        {
            if (IPHost.TryParse(endpoint, out IPHost? ep))
            {
                // ep is not null as TryParse returned true.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference : If tryParse returns true, ep is not null.
                lock (_intLock)
                {
                    // If LAN addresses haven't been defined, the code uses interface addresses.
                    if (_usingInterfaces)
                    {
                        // Ensure we're an internal address.
                        return _filteredLANAddresses.Contains(ep) && ep.IsPrivateAddressRange();
                    }

                    return _filteredLANAddresses.Contains(ep);
                }
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.

            return false;
        }

        /// <summary>
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        public bool IsInLocalNetwork(IPNetAddress endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            lock (_intLock)
            {
                // If LAN addresses haven't been defined, the code uses interface addresses.
                if (_usingInterfaces)
                {
                    // Ensure we're an internal address.
                    return _filteredLANAddresses.Contains(endpoint) && endpoint.IsPrivateAddressRange();
                }

                return _filteredLANAddresses.Contains(endpoint);
            }
        }

        /// <summary>
        /// Parses an array of strings into a NetCollection.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <param name="bracketed">When true, only include values in []. When false, ignore bracketed values.</param>
        /// <returns>IPCollection object containing the value strings.</returns>
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
                        if (v.StartsWith("[", StringComparison.OrdinalIgnoreCase) && v.EndsWith("]", StringComparison.OrdinalIgnoreCase))
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
                        _logger?.LogInformation("Ignoring LAN value {value}. Reason : {reason}", v, e.Message);
                    }
                }
            }

            return col;
        }

        /// <summary>
        /// Returns all the valid interfaces in config LocalNetworkAddresses.
        /// </summary>
        /// <returns>A NetCollection object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items.</returns>
        public NetCollection GetBindInterfaces()
        {
            lock (_intLock)
            {
                if (_bindAddresses.Count == 0)
                {
                    if (_bindExclusions.Count > 0)
                    {
                        // Return all the interfaces except the one excluded.
                        return new NetCollection(_interfaceAddresses.Exclude(_bindExclusions));
                    }

                    // Return no interfaces.
                    return new NetCollection();
                }

                // Return only interface addresses that are valid.
                if (_bindAddresses.Equals(_interfaceAddresses))
                {
                    // If bindAddress == interfaceAddresses then listen on any address.
                    return new NetCollection();
                }

                // Otherwise, return only valid interface addresses.
                return new NetCollection(_bindAddresses.Union(_interfaceAddresses));
            }
        }

        /// <summary>
        /// Returns all the excluded interfaces in config LocalNetworkAddresses.
        /// </summary>
        /// <returns>A NetCollection object containing all the excluded interfaces.</returns>
        public NetCollection GetBindExclusions()
        {
            lock (_intLock)
            {
                return new NetCollection(_bindExclusions.Union(_interfaceAddresses));
            }
        }

        /// <summary>
        /// Gets the filtered LAN ip networt addresses.
        /// </summary>
        /// <param name="filter">Filter for the list.</param>
        /// <returns>Returns a filtered list of LAN addresses.</returns>
        public NetCollection GetFilteredLANAddresses(NetCollection? filter = null)
        {
            lock (_intLock)
            {
                if (filter == null)
                {
                    return new NetCollection(_filteredLANAddresses);
                }

                return new NetCollection(_lanAddresses.Exclude(filter));
            }
        }

        /// <summary>
        /// Checks to see if an IP address is still a valid interface address.
        /// </summary>
        /// <param name="address">IP address to check.</param>
        /// <returns>True if it is.</returns>
        public bool IsValidInterfaceAddress(IPAddress address)
        {
            lock (_intLock)
            {
                return _interfaceAddresses.Exists(address);
            }
        }

        /// <summary>
        /// Returns all the filtered LAN interfaces addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        public NetCollection GetInternalInterfaceAddresses()
        {
            lock (_intLock)
            {
                return new NetCollection(_internalInterfaceAddresses);
            }
        }

        /// <summary>
        /// Interface callback function that returns the IP address of the first callback that succeeds.
        /// </summary>
        /// <param name="callback">Delegate function to call for each ip.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>NetCollection object.</returns>
        public NetCollection OnFilteredBindAddressesCallback(
            Func<IPObject, CancellationToken, Task<bool>> callback,
            CancellationToken cancellationToken)
        {
            NetCollection interfaces = GetBindInterfaces();
            // If GetBindInterfaces returns zero items for all interfaces,
            // we require all the individual interfaces.
            if (interfaces.Count == 0)
            {
                lock (_intLock)
                {
                    interfaces = new NetCollection(_interfaceAddresses);
                }
            }

            try
            {
                return interfaces.Callback(callback, cancellationToken, 1);
            }
            catch (AggregateException ae)
            {
                foreach (var a in ae.InnerExceptions)
                {
                    _logger.LogError(a, "Error in callback thread.");
                }
            }

            interfaces.Clear();
            return interfaces;
        }

        /// <summary>
        /// Parses strings into the collection, replacing any interface references.
        /// </summary>
        /// <param name="col">Collection.</param>
        /// <param name="token">String to parse.</param>
        internal void AddToCollection(NetCollection col, string token)
        {
            // Is it the name of an interface (windows) eg, Wireless LAN adapter Wireless Network Connection 1.
            if (_interfaceNames.TryGetValue(token.ToLower(CultureInfo.InvariantCulture), out int index))
            {
                _logger?.LogInformation("Interface {0} used in settings. Using its interface addresses.", token);

                // Replace interface tags with the interface IP's.
                foreach (IPNetAddress iface in _interfaceAddresses)
                {
                    if (iface.Tag == index
                        && ((!IsIP6Enabled && iface.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            || IsIP6Enabled))
                    {
                        col.Add(iface);
                    }
                }
            }
            else if (NetCollection.TryParse(token, out IPObject? obj))
            {
                // TryParse returned true, so obj is non-null

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
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
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else
            {
                _logger?.LogDebug("Invalid or unknown network {0}.", token);
            }
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Network availablity information.</param>
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _logger?.LogDebug("NetworkAvailabilityChanged");
            OnNetworkChanged();
        }

        /// <summary>
        /// Handler for network change events.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            _logger?.LogDebug("NetworkAddressChanged");
            OnNetworkChanged();
        }

        /// <summary>
        /// Async task that waits for 2 seconds before re-initialising this class.
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
                // Don't know if we need to do this - but it won't hurt.
                InitialiseBind();

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
        /// Reparses the Bind address settings.
        /// </summary>
        private void InitialiseBind()
        {
            string[] ba = _bindAddressesFn();
            lock (_intLock)
            {
                _bindAddresses = CreateIPCollection(ba);
                _bindExclusions = CreateIPCollection(ba, true);
            }
        }

        /// <summary>
        /// Initialises internal variables.
        /// </summary>
        private void InitialiseLAN()
        {
            lock (_intLock)
            {
                _logger?.LogDebug("Refreshing LAN information.");

                // Get config options.
                string[] subnets = _localSubnetsFn();

                // Create lists from user settings.
                _lanAddresses = NetCollection.AsNetworks(CreateIPCollection(subnets));
                // If no LAN addresses are specified - all interface subnets are deemed to be the LAN
                _usingInterfaces = _lanAddresses.Count == 0;

                _excludedAddresses = CreateIPCollection(subnets, true);

                _logger?.LogDebug("# User defined LAN addresses : {0}", _lanAddresses);
                _logger?.LogDebug("# User defined LAN exclusions : {0}", _excludedAddresses);

                if (_usingInterfaces)
                {
                    _logger?.LogDebug("Using interface addresses as user provided no LAN details.");
                    _lanAddresses = new NetCollection(_interfaceAddresses);
                }

                // Cache results.
                if (_excludedAddresses.Count > 0)
                {
                    _internalInterfaceAddresses = new NetCollection(_interfaceAddresses.Exclude(_excludedAddresses).Where(i => i.IsPrivateAddressRange()));
                    _filteredLANAddresses = NetCollection.AsNetworks(_lanAddresses.Exclude(_excludedAddresses));
                }
                else
                {
                    _internalInterfaceAddresses = new NetCollection(_interfaceAddresses.Where(i => i.IsPrivateAddressRange()));
                    _filteredLANAddresses = NetCollection.AsNetworks(_lanAddresses);
                }

                _logger?.LogDebug("Using LAN addresses: {0}", _filteredLANAddresses);
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
                _logger?.LogDebug("Refreshing interfaces.");

                _interfaceNames.Clear();
                _interfaceAddresses.Clear();

                // retrieve a list of network interfaces that are up or unknown? (why unknown???)
                try
                {
                    bool ip6 = _isIP6EnabledFn();

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
                                    _interfaceAddresses.Add(nw);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Name.ToLower(CultureInfo.InvariantCulture)] = (int)nw.Tag;
                                    _interfaceNames["eth" + nw.Tag.ToString(CultureInfo.InvariantCulture)] = (int)nw.Tag;
                                }
                                else if (ip6 && info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    IPNetAddress nw = new IPNetAddress(info.Address)
                                    {
                                        // Keep the number of gateways on this interface, along with its index.
                                        Tag = ipProperties.GetIPv6Properties().Index
                                    };
                                    _interfaceAddresses.Add(nw);

                                    // Store interface name so we can use the name in Collections.
                                    _interfaceNames[adapter.Name.ToLower(CultureInfo.InvariantCulture)] = (int)nw.Tag;
                                    _interfaceNames["eth" + nw.Tag.ToString(CultureInfo.InvariantCulture)] = (int)nw.Tag;
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

                    _logger?.LogDebug("Discovered {0} interfaces.", _interfaceAddresses.Count);

                    // If for some reason we don't have an interface info, resolve our DNS name.
                    if (_interfaceAddresses.Count == 0)
                    {
                        _logger?.LogWarning("No interfaces information available. Using loopback.");

                        IPHost host = new IPHost(Dns.GetHostName());
                        foreach (var a in host.Addresses)
                        {
                            _interfaceAddresses.Add(a);
                        }

                        if (_interfaceAddresses.Count == 0)
                        {
                            _logger?.LogError("No interfaces information available. Resolving DNS name.");
                            // Last ditch attempt - use loopback.
                            _interfaceAddresses.Add(IPAddress.Parse("127.0.0.1"));
                        }
                    }
                }
                catch (NetworkInformationException ex)
                {
                    _logger?.LogError(ex, "Error in InitialiseInterfaces.");
                }
            }
        }
    }
}
