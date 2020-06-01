using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Common.Networking
{
    /// <summary>
    /// Class to take care of network interface management.
    /// </summary>
    public class NetworkManager
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Threading object for network interfaces.
        /// </summary>
        private readonly object _intLock = new object();

        /// <summary>
        /// List of calculated LAN addresses.
        /// </summary>
        private NetCollection _lanAddresses;

        /// <summary>
        /// Cached list of filtered LAN addresses.
        /// </summary>
        private NetCollection _filteredLANAddresses;

        /// <summary>
        /// List of LAN excluded addresses.
        /// </summary>
        private NetCollection _excludedAddresses;

        /// <summary>
        /// List of all interface addresses and masks.
        /// </summary>
        private NetCollection _interfaceAddresses;

        /// <summary>
        /// Caches list of all filtered interface addresses and masks.
        /// </summary>
        private NetCollection _filteredInterfaceAddresses;

        /// <summary>
        /// List of all interface mac addresses.
        /// </summary>
        private List<PhysicalAddress> _macAddresses;

        /// <summary>
        /// Flag set when _lanAddressses is set to _interfaceAddresses as no custom LAN has been defined in the config.
        /// </summary>
        private bool _usingInterfaces = false;

        /// <summary>
        /// Function that return the LAN addresses from the config.
        /// </summary>
        private Func<string[]> _localSubnetsFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        /// <param name="logger">Logger to use for messages.</param>
        public NetworkManager(ILogger<NetworkManager> logger)
        {
            _logger = logger;
            InitialiseInterfaces();
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        /// <summary>
        /// Event triggered on network changes.
        /// </summary>
        public event EventHandler NetworkChanged;

        /// <summary>
        /// Gets or sets the LAN settings stored in config.
        /// </summary>
        public Func<string[]> LocalSubnetsFn
        {
            get
            {
                return _localSubnetsFn;
            }

            set
            {
                _localSubnetsFn = value;
                InitialiseLAN();
            }
        }

        /// <summary>
        /// Event triggered when configuration is changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">New configuration.</param>
        public void NamedConfigurationUpdated(object sender, EventArgs e)
        {
            InitialiseLAN();
        }

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetRandomUnusedTcpPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetRandomUnusedUdpPort()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            using (var udpClient = new UdpClient(localEndPoint))
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
        /// Returns true if the IP address in address2 is within the network address1/subnetMask.
        /// </summary>
        /// <param name="subnetIP">Subnet IP.</param>
        /// <param name="subnetMask">Subnet Mask.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if address is in the subnet.</returns>
        public static bool IsInSameSubnet(IPAddress subnetIP, IPAddress subnetMask, IPAddress address)
        {
            return IPNetAddress.NetworkAddress(subnetIP, subnetMask) == IPNetAddress.NetworkAddress(address, subnetMask);
        }

        /// <summary>
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        public bool IsInLocalNetwork(string endpoint)
        {
            if (IPHost.TryParse(endpoint, out IPHost ep))
            {
                lock (_intLock)
                {
                    // If LAN addresses haven't been defined, the code uses interface addresses.
                    if (_usingInterfaces)
                    {
                        // Ensure we're an internal address.
                        return _filteredLANAddresses.Contains(ep) && ep.IsPrivateAddressRange();
                    }
                    else
                    {
                        return _filteredLANAddresses.Contains(ep);
                    }
                }
            }

            return false;
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
                                col.Add(v[1..^1]);
                            }
                        }
                        else
                        {
                            if (!bracketed)
                            {
                                col.Add(v);
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

        /// <summary>
        /// Gets the filtered LAN ip addresses.
        /// </summary>
        /// <param name="filter">Filter for the list.</param>
        /// <returns>Returns a filtered list of LAN addresses.</returns>
        public NetCollection GetFilteredLANAddresses(NetCollection filter)
        {
            lock (_intLock)
            {
                return new NetCollection(_lanAddresses.Exclude(filter));
            }
        }

        /// <summary>
        ///  Returns all the filtered LAN addresses.
        /// </summary>
        /// <returns>A filtered list of LAN subnets/IPs.</returns>
        public NetCollection GetLANAddresses()
        {
            lock (_intLock)
            {
                return new NetCollection(_lanAddresses);
            }
        }

        /// <summary>
        /// Returns all the interfaces filtered by filter.
        /// </summary>
        /// <param name="filter">The list to filter the interfaces by.</param>
        /// <returns>The resultant set.</returns>
        public NetCollection GetInterfaceAddresses(NetCollection filter)
        {
            lock (_intLock)
            {
                return new NetCollection(_interfaceAddresses.Exclude(filter));
            }
        }

        /// <summary>
        /// Returns all the filtered interfaces addresses.
        /// </summary>
        /// <returns>A filtered list of interfaces addresses.</returns>
        public NetCollection GetFilteredInterfaceAddresses()
        {
            lock (_intLock)
            {
                return new NetCollection(_filteredInterfaceAddresses);
            }
        }

        /// <summary>
        /// Returns all filtered interface addresses.
        /// </summary>
        /// <returns>Returns a filtered list of IPV4 interface addresses.</returns>
        public NetCollection GetFilteredIPv4InterfaceAddresses()
        {
            lock (_intLock)
            {
                return new NetCollection(_filteredInterfaceAddresses
                    .Where(i => ((IPNetAddress)i).Address.AddressFamily == AddressFamily.InterNetwork));
            }
        }

        /// <summary>
        /// Returns all filtered interface addresses that respond to ping.
        /// </summary>
        /// <param name="allowLoopback">Allow loopback addresses in the list.</param>
        /// <param name="limit">Limit the number of items in the response.</param>
        /// <returns>Returns a filtered list of IPV4 interface addresses.</returns>
        public NetCollection GetPingableIPv4InterfaceAddresses(bool allowLoopback, int limit)
        {
            lock (_intLock)
            {
                return new NetCollection(_filteredInterfaceAddresses
                    .Where(i => ((IPNetAddress)i).Address.AddressFamily == AddressFamily.InterNetwork)
                    .Where(i => allowLoopback || !i.IsLoopback())
                    .Take(limit));
            }
        }

        /// <summary>
        /// Interface callback function.
        /// </summary>
        /// <param name="allowLoopback">Allow loopback addresses in the list.</param>
        /// <param name="limit">Limit the number of items in the response.</param>
        /// <param name="callback">Delegate function to call on each match.</param>
        /// <param name="cancellationToken">Cancellation Tolken.</param>
        /// <returns>true or false.</returns>
        public NetCollection GetCallbackFilteredInterfaceAddresses(bool allowLoopback, int limit, Func<IPAddress, CancellationToken, Task<bool>> callback, CancellationToken cancellationToken)
        {
            lock (_intLock)
            {
                return new NetCollection(_filteredInterfaceAddresses
                    .Where(i => allowLoopback || !i.IsLoopback())
                    .Where(i => callback(((IPNetAddress)i).Address, cancellationToken).Result)
                    .Take(limit));
            }
        }

        /// <summary>
        /// Initialises internal variables.
        /// </summary>
        private void InitialiseLAN()
        {
            lock (_intLock)
            {
                _excludedAddresses = CreateIPCollection(LocalSubnetsFn(), true);

                // Collate and cache all the LAN network information.
                _lanAddresses = CreateIPCollection(LocalSubnetsFn(), false);

                if (_excludedAddresses.Count > 0)
                {
                    _filteredInterfaceAddresses = _interfaceAddresses.Exclude(_excludedAddresses);
                    _filteredLANAddresses = _lanAddresses.Exclude(_excludedAddresses);
                }
                else
                {
                    _filteredInterfaceAddresses = _interfaceAddresses;
                    _filteredLANAddresses = _lanAddresses;
                }

                // If no LAN addresses are specified - all interface subnets are deemed to be the LAN
                _usingInterfaces = _lanAddresses.Count == 0;
                if (_usingInterfaces)
                {
                    foreach (IPObject i in _interfaceAddresses)
                    {
                        if (i is IPNetAddress nw)
                        {
                            // Add the subnet calculated from the interface address/mask.
                            IPNetAddress lan = new IPNetAddress(IPNetAddress.NetworkAddress(nw.Address, nw.Mask), nw.Mask)
                            {
                                Tag = i.Tag
                            };
                            _lanAddresses.Add(lan);
                        }
                        else
                        {
                            // Flatten out IPHost and add all its ip addresses.
                            foreach (var addr in ((IPHost)i).Addresses)
                            {
                                IPNetAddress host = new IPNetAddress(addr, 32)
                                {
                                    Tag = i.Tag
                                };
                                _lanAddresses.Add(host);
                            }
                        }
                    }
                }
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
        /// Triggers our event, and re-loads interface information.
        /// </summary>
        private void OnNetworkChanged()
        {
            InitialiseInterfaces();
            NetworkChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Generate a list of all the interface ip addresses and submasks where that are in the active/unknown state.
        /// Generate a list of all active mac addresses that aren't loopback addreses.
        /// </summary>
        private void InitialiseInterfaces()
        {
            lock (_intLock)
            {
                _interfaceAddresses = new NetCollection();
                _macAddresses = new List<PhysicalAddress>();

                // retrieve a list of network interfaces that are up or unknown? (why unknown???)
                try
                {
                    IEnumerable<NetworkInterface> nics = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(x => x.OperationalStatus == OperationalStatus.Up
                            || x.OperationalStatus == OperationalStatus.Unknown);

                    foreach (NetworkInterface adapter in nics)
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
                                    Tag = ((long)ipProperties.GetIPv4Properties().Index << 32) + ipProperties.GatewayAddresses.Count
                                };
                                _interfaceAddresses.Add(nw);
                            }
                            else if (info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                IPNetAddress nw = new IPNetAddress(info.Address)
                                {
                                    // Keep the number of gateways on this interface, along with its index.
                                    Tag = ((long)ipProperties.GetIPv6Properties().Index << 32) + ipProperties.GatewayAddresses.Count
                                };
                                _interfaceAddresses.Add(nw);
                            }
                        }
                    }

                    // If for some reason we don't have an interface info, resolve our DNS name.
                    if (_interfaceAddresses.Count == 0)
                    {
                        IPHost host = new IPHost(Dns.GetHostName());
                        foreach (var a in host.Addresses)
                        {
                            _interfaceAddresses.Add(a);
                        }
                    }
                }
                catch (NetworkInformationException ex)
                {
                    _logger.LogError(ex, "Error in GetAllNetworkInterfaces");
                }
            }
        }
    }
}
