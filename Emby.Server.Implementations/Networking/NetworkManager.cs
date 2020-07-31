#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Networking
{
    /// <summary>
    /// Class to take care of network interface management.
    /// </summary>
    public class NetworkManager : INetworkManager
    {
        private readonly ILogger<NetworkManager> _logger;

        private IPAddress[] _localIpAddresses;
        private readonly object _localIpAddressSyncLock = new object();

        private readonly object _subnetLookupLock = new object();
        private readonly Dictionary<string, List<string>> _subnetLookup = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private List<PhysicalAddress> _macAddresses;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkManager"/> class.
        /// </summary>
        /// <param name="logger">Logger to use for messages.</param>
        public NetworkManager(ILogger<NetworkManager> logger)
        {
            _logger = logger;

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        /// <inheritdoc/>
        public event EventHandler NetworkChanged;

        /// <inheritdoc/>
        public Func<string[]> LocalSubnetsFn { get; set; }

        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            _logger.LogDebug("NetworkAvailabilityChanged");
            OnNetworkChanged();
        }

        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            _logger.LogDebug("NetworkAddressChanged");
            OnNetworkChanged();
        }

        private void OnNetworkChanged()
        {
            lock (_localIpAddressSyncLock)
            {
                _localIpAddresses = null;
                _macAddresses = null;
            }

            NetworkChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public IPAddress[] GetLocalIpAddresses()
        {
            lock (_localIpAddressSyncLock)
            {
                if (_localIpAddresses == null)
                {
                    var addresses = GetLocalIpAddressesInternal().ToArray();

                    _localIpAddresses = addresses;
                }

                return _localIpAddresses;
            }
        }

        private List<IPAddress> GetLocalIpAddressesInternal()
        {
            var list = GetIPsDefault().ToList();

            if (list.Count == 0)
            {
                list = GetLocalIpAddressesFallback().GetAwaiter().GetResult().ToList();
            }

            var listClone = new List<IPAddress>();

            var subnets = LocalSubnetsFn();

            foreach (var i in list)
            {
                if (i.IsIPv6LinkLocal || i.ToString().StartsWith("169.254.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Array.IndexOf(subnets, $"[{i}]") == -1)
                {
                    listClone.Add(i);
                }
            }

            return listClone
                .OrderBy(i => i.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                // .ThenBy(i => listClone.IndexOf(i))
                .GroupBy(i => i.ToString())
                .Select(x => x.First())
                .ToList();
        }

        /// <inheritdoc/>
        public bool IsInPrivateAddressSpace(string endpoint)
        {
            return IsInPrivateAddressSpace(endpoint, true);
        }

        // Checks if the address in endpoint is an RFC1918, RFC1122, or RFC3927 address
        private bool IsInPrivateAddressSpace(string endpoint, bool checkSubnets)
        {
            if (string.Equals(endpoint, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // IPV6
            if (endpoint.Split('.').Length > 4)
            {
                // Handle ipv4 mapped to ipv6
                var originalEndpoint = endpoint;
                endpoint = endpoint.Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase);

                if (string.Equals(endpoint, originalEndpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // Private address space:

            if (string.Equals(endpoint, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!IPAddress.TryParse(endpoint, out var ipAddress))
            {
                return false;
            }

            byte[] octet = ipAddress.GetAddressBytes();

            if ((octet[0] == 10) ||
                (octet[0] == 172 && (octet[1] >= 16 && octet[1] <= 31)) || // RFC1918
                (octet[0] == 192 && octet[1] == 168) || // RFC1918
                (octet[0] == 127) || // RFC1122
                (octet[0] == 169 && octet[1] == 254)) // RFC3927
            {
                return true;
            }

            if (checkSubnets && IsInPrivateAddressSpaceAndLocalSubnet(endpoint))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsInPrivateAddressSpaceAndLocalSubnet(string endpoint)
        {
            if (endpoint.StartsWith("10.", StringComparison.OrdinalIgnoreCase))
            {
                var endpointFirstPart = endpoint.Split('.')[0];

                var subnets = GetSubnets(endpointFirstPart);

                foreach (var subnet_Match in subnets)
                {
                    // logger.LogDebug("subnet_Match:" + subnet_Match);

                    if (endpoint.StartsWith(subnet_Match + ".", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Gives a list of possible subnets from the system whose interface ip starts with endpointFirstPart
        private List<string> GetSubnets(string endpointFirstPart)
        {
            lock (_subnetLookupLock)
            {
                if (_subnetLookup.TryGetValue(endpointFirstPart, out var subnets))
                {
                    return subnets;
                }

                subnets = new List<string>();

                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (var unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork && endpointFirstPart == unicastIPAddressInformation.Address.ToString().Split('.')[0])
                        {
                            int subnet_Test = 0;
                            foreach (string part in unicastIPAddressInformation.IPv4Mask.ToString().Split('.'))
                            {
                                if (part.Equals("0", StringComparison.Ordinal))
                                {
                                    break;
                                }

                                subnet_Test++;
                            }

                            var subnet_Match = string.Join(".", unicastIPAddressInformation.Address.ToString().Split('.').Take(subnet_Test).ToArray());

                            // TODO: Is this check necessary?
                            if (adapter.OperationalStatus == OperationalStatus.Up)
                            {
                                subnets.Add(subnet_Match);
                            }
                        }
                    }
                }

                _subnetLookup[endpointFirstPart] = subnets;

                return subnets;
            }
        }

        /// <inheritdoc/>
        public bool IsInLocalNetwork(string endpoint)
        {
            return IsInLocalNetworkInternal(endpoint, true);
        }

        /// <inheritdoc/>
        public bool IsAddressInSubnets(string addressString, string[] subnets)
        {
            return IsAddressInSubnets(IPAddress.Parse(addressString), addressString, subnets);
        }

        /// <inheritdoc/>
        public bool IsAddressInSubnets(IPAddress address, bool excludeInterfaces, bool excludeRFC)
        {
            byte[] octet = address.GetAddressBytes();

            if ((octet[0] == 127) || // RFC1122
                (octet[0] == 169 && octet[1] == 254)) // RFC3927
            {
                // don't use on loopback or 169 interfaces
                return false;
            }

            string addressString = address.ToString();
            string excludeAddress = "[" + addressString + "]";
            var subnets = LocalSubnetsFn();

            // Include any address if LAN subnets aren't specified
            if (subnets.Length == 0)
            {
                return true;
            }

            // Exclude any addresses if they appear in the LAN list in [ ]
            if (Array.IndexOf(subnets, excludeAddress) != -1)
            {
                return false;
            }

            return IsAddressInSubnets(address, addressString, subnets);
        }

        /// <summary>
        /// Checks if the give address falls within the ranges given in [subnets]. The addresses in subnets can be hosts or subnets in the CIDR format.
        /// </summary>
        /// <param name="address">IPAddress version of the address.</param>
        /// <param name="addressString">The address to check.</param>
        /// <param name="subnets">If true, check against addresses in the LAN settings which have [] arroud and return true if it matches the address give in address.</param>
        /// <returns><c>false</c>if the address isn't in the subnets, <c>true</c> otherwise.</returns>
        private static bool IsAddressInSubnets(IPAddress address, string addressString, string[] subnets)
        {
            foreach (var subnet in subnets)
            {
                var normalizedSubnet = subnet.Trim();
                // Is the subnet a host address and does it match the address being passes?
                if (string.Equals(normalizedSubnet, addressString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Parse CIDR subnets and see if address falls within it.
                if (normalizedSubnet.Contains('/', StringComparison.Ordinal))
                {
                    try
                    {
                        var ipNetwork = IPNetwork.Parse(normalizedSubnet);
                        if (ipNetwork.Contains(address))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Ignoring - invalid subnet passed encountered.
                    }
                }
            }

            return false;
        }

        private bool IsInLocalNetworkInternal(string endpoint, bool resolveHost)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (IPAddress.TryParse(endpoint, out var address))
            {
                var addressString = address.ToString();

                var localSubnetsFn = LocalSubnetsFn;
                if (localSubnetsFn != null)
                {
                    var localSubnets = localSubnetsFn();
                    foreach (var subnet in localSubnets)
                    {
                        // Only validate if there's at least one valid entry.
                        if (!string.IsNullOrWhiteSpace(subnet))
                        {
                            return IsAddressInSubnets(address, addressString, localSubnets) || IsInPrivateAddressSpace(addressString, false);
                        }
                    }
                }

                int lengthMatch = 100;
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    lengthMatch = 4;
                    if (IsInPrivateAddressSpace(addressString, true))
                    {
                        return true;
                    }
                }
                else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    lengthMatch = 9;
                    if (IsInPrivateAddressSpace(endpoint, true))
                    {
                        return true;
                    }
                }

                // Should be even be doing this with ipv6?
                if (addressString.Length >= lengthMatch)
                {
                    var prefix = addressString.Substring(0, lengthMatch);

                    if (GetLocalIpAddresses().Any(i => i.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            else if (resolveHost)
            {
                if (Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out var uri))
                {
                    try
                    {
                        var host = uri.DnsSafeHost;
                        _logger.LogDebug("Resolving host {0}", host);

                        address = GetIpAddresses(host).Result.FirstOrDefault();

                        if (address != null)
                        {
                            _logger.LogDebug("{0} resolved to {1}", host, address);

                            return IsInLocalNetworkInternal(address.ToString(), false);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Can happen with reverse proxy or IIS url rewriting?
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resolving hostname");
                    }
                }
            }

            return false;
        }

        private static Task<IPAddress[]> GetIpAddresses(string hostName)
        {
            return Dns.GetHostAddressesAsync(hostName);
        }

        private IEnumerable<IPAddress> GetIPsDefault()
        {
            IEnumerable<NetworkInterface> interfaces;

            try
            {
                interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.OperationalStatus == OperationalStatus.Up
                        || x.OperationalStatus == OperationalStatus.Unknown);
            }
            catch (NetworkInformationException ex)
            {
                _logger.LogError(ex, "Error in GetAllNetworkInterfaces");
                return Enumerable.Empty<IPAddress>();
            }

            return interfaces.SelectMany(network =>
            {
                var ipProperties = network.GetIPProperties();

                // Exclude any addresses if they appear in the LAN list in [ ]

                return ipProperties.UnicastAddresses
                    .Select(i => i.Address)
                    .Where(i => i.AddressFamily == AddressFamily.InterNetwork || i.AddressFamily == AddressFamily.InterNetworkV6);
            }).GroupBy(i => i.ToString())
                .Select(x => x.First());
        }

        private static async Task<IEnumerable<IPAddress>> GetLocalIpAddressesFallback()
        {
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName()).ConfigureAwait(false);

            // Reverse them because the last one is usually the correct one
            // It's not fool-proof so ultimately the consumer will have to examine them and decide
            return host.AddressList
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork || i.AddressFamily == AddressFamily.InterNetworkV6)
                .Reverse();
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

        /// <inheritdoc/>
        public int GetRandomUnusedUdpPort()
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            using (var udpClient = new UdpClient(localEndPoint))
            {
                return ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
            }
        }

        /// <inheritdoc/>
        public List<PhysicalAddress> GetMacAddresses()
        {
            return _macAddresses ??= GetMacAddressesInternal().ToList();
        }

        private static IEnumerable<PhysicalAddress> GetMacAddressesInternal()
            => NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(x => x.GetPhysicalAddress())
                .Where(x => !x.Equals(PhysicalAddress.None));

        /// <inheritdoc/>
        public bool IsInSameSubnet(IPAddress address1, IPAddress address2, IPAddress subnetMask)
        {
            IPAddress network1 = GetNetworkAddress(address1, subnetMask);
            IPAddress network2 = GetNetworkAddress(address2, subnetMask);
            return network1.Equals(network2);
        }

        private IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
            {
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
            }

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & subnetMaskBytes[i]);
            }

            return new IPAddress(broadcastAddress);
        }

         /// <inheritdoc/>
        public IPAddress GetLocalIpSubnetMask(IPAddress address)
        {
            NetworkInterface[] interfaces;

            try
            {
                var validStatuses = new[] { OperationalStatus.Up, OperationalStatus.Unknown };

                interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => validStatuses.Contains(i.OperationalStatus))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllNetworkInterfaces");
                return null;
            }

            foreach (NetworkInterface ni in interfaces)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.Equals(address) && ip.IPv4Mask != null)
                    {
                        return ip.IPv4Mask;
                    }
                }
            }

            return null;
        }
    }
}
