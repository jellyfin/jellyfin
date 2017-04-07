using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Net;

namespace Emby.Common.Implementations.Networking
{
    public class NetworkManager : INetworkManager
    {
        protected ILogger Logger { get; private set; }
        private DateTime _lastRefresh;

        public NetworkManager(ILogger logger)
        {
            Logger = logger;
        }

        private List<IpAddressInfo> _localIpAddresses;
        private readonly object _localIpAddressSyncLock = new object();

        public List<IpAddressInfo> GetLocalIpAddresses()
        {
            const int cacheMinutes = 5;

            lock (_localIpAddressSyncLock)
            {
                var forceRefresh = (DateTime.UtcNow - _lastRefresh).TotalMinutes >= cacheMinutes;

                if (_localIpAddresses == null || forceRefresh)
                {
                    var addresses = GetLocalIpAddressesInternal().Select(ToIpAddressInfo).ToList();

                    _localIpAddresses = addresses;
                    _lastRefresh = DateTime.UtcNow;

                    return addresses;
                }
            }

            return _localIpAddresses;
        }

        private IEnumerable<IPAddress> GetLocalIpAddressesInternal()
        {
            var list = GetIPsDefault()
                .ToList();

            if (list.Count == 0)
            {
                list.AddRange(GetLocalIpAddressesFallback().Result);
            }

            return list.Where(FilterIpAddress).DistinctBy(i => i.ToString());
        }

        private bool FilterIpAddress(IPAddress address)
        {
            var addressString = address.ToString();

            if (addressString.StartsWith("169.", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public bool IsInPrivateAddressSpace(string endpoint)
        {
            if (string.Equals(endpoint, "::1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Handle ipv4 mapped to ipv6
            endpoint = endpoint.Replace("::ffff:", string.Empty);

            // Private address space:
            // http://en.wikipedia.org/wiki/Private_network

            if (endpoint.StartsWith("172.", StringComparison.OrdinalIgnoreCase))
            {
                return Is172AddressPrivate(endpoint);
            }

            return

                endpoint.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("192.168", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("169.", StringComparison.OrdinalIgnoreCase);
        }

        private bool Is172AddressPrivate(string endpoint)
        {
            for (var i = 16; i <= 31; i++)
            {
                if (endpoint.StartsWith("172." + i.ToString(CultureInfo.InvariantCulture) + ".", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsInLocalNetwork(string endpoint)
        {
            return IsInLocalNetworkInternal(endpoint, true);
        }

        public bool IsInLocalNetworkInternal(string endpoint, bool resolveHost)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException("endpoint");
            }

            IPAddress address;
            if (IPAddress.TryParse(endpoint, out address))
            {
                var addressString = address.ToString();

                int lengthMatch = 100;
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    lengthMatch = 4;
                    if (IsInPrivateAddressSpace(addressString))
                    {
                        return true;
                    }
                }
                else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    lengthMatch = 10;
                    if (IsInPrivateAddressSpace(endpoint))
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
                Uri uri;
                if (Uri.TryCreate(endpoint, UriKind.RelativeOrAbsolute, out uri))
                {
                    try
                    {
                        var host = uri.DnsSafeHost;
                        Logger.Debug("Resolving host {0}", host);

                        address = GetIpAddresses(host).Result.FirstOrDefault();

                        if (address != null)
                        {
                            Logger.Debug("{0} resolved to {1}", host, address);

                            return IsInLocalNetworkInternal(address.ToString(), false);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Can happen with reverse proxy or IIS url rewriting
                    }
                    catch (Exception ex)
                    {
                        Logger.ErrorException("Error resovling hostname", ex);
                    }
                }
            }

            return false;
        }

        private Task<IPAddress[]> GetIpAddresses(string hostName)
        {
            return Dns.GetHostAddressesAsync(hostName);
        }

        private readonly List<NetworkInterfaceType> _validNetworkInterfaceTypes = new List<NetworkInterfaceType>
        {
            NetworkInterfaceType.Ethernet,
            NetworkInterfaceType.Wireless80211
        };

        private List<IPAddress> GetIPsDefault()
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
                Logger.ErrorException("Error in GetAllNetworkInterfaces", ex);
                return new List<IPAddress>();
            }

            return interfaces.SelectMany(network =>
            {

                try
                {
                    Logger.Debug("Querying interface: {0}. Type: {1}. Status: {2}", network.Name, network.NetworkInterfaceType, network.OperationalStatus);

                    var ipProperties = network.GetIPProperties();

                    // Try to exclude virtual adapters
                    // http://stackoverflow.com/questions/8089685/c-sharp-finding-my-machines-local-ip-address-and-not-the-vms
                    var addr = ipProperties.GatewayAddresses.FirstOrDefault();
                    if (addr == null || string.Equals(addr.Address.ToString(), "0.0.0.0", StringComparison.OrdinalIgnoreCase))
                    {
                        return new List<IPAddress>();
                    }

                    //if (!_validNetworkInterfaceTypes.Contains(network.NetworkInterfaceType))
                    //{
                    //    return new List<IPAddress>();
                    //}

                    return ipProperties.UnicastAddresses
                        //.Where(i => i.IsDnsEligible)
                        .Select(i => i.Address)
                        .Where(i => i.AddressFamily == AddressFamily.InterNetwork)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error querying network interface", ex);
                    return new List<IPAddress>();
                }

            }).DistinctBy(i => i.ToString())
                .ToList();
        }

        private async Task<IEnumerable<IPAddress>> GetLocalIpAddressesFallback()
        {
            var host = await Dns.GetHostEntryAsync(Dns.GetHostName()).ConfigureAwait(false);

            // Reverse them because the last one is usually the correct one
            // It's not fool-proof so ultimately the consumer will have to examine them and decide
            return host.AddressList
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork)
                .Reverse();
        }

        /// <summary>
        /// Gets a random port number that is currently available
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

        public int GetRandomUnusedUdpPort()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            using (var udpClient = new UdpClient(localEndPoint))
            {
                var port = ((IPEndPoint)(udpClient.Client.LocalEndPoint)).Port;
                return port;
            }
        }

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public string GetMacAddress()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(i => BitConverter.ToString(i.GetPhysicalAddress().GetAddressBytes()))
                .FirstOrDefault();
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <returns>IPEndPoint.</returns>
        public IPEndPoint Parse(string endpointstring)
        {
            return Parse(endpointstring, -1).Result;
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <param name="defaultport">The defaultport.</param>
        /// <returns>IPEndPoint.</returns>
        /// <exception cref="System.ArgumentException">Endpoint descriptor may not be empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        private static async Task<IPEndPoint> Parse(string endpointstring, int defaultport)
        {
            if (String.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                || defaultport > IPEndPoint.MaxPort))
            {
                throw new ArgumentException(String.Format("Invalid default port '{0}'", defaultport));
            }

            string[] values = endpointstring.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                port = values.Length == 1 ? defaultport : GetPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = await GetIPfromHost(values[0]).ConfigureAwait(false);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = String.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = GetPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpointstring);
                    port = defaultport;
                }
            }
            else
            {
                throw new FormatException(String.Format("Invalid endpoint ipaddress '{0}'", endpointstring));
            }

            if (port == -1)
                throw new ArgumentException(String.Format("No port specified: '{0}'", endpointstring));

            return new IPEndPoint(ipaddy, port);
        }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.FormatException"></exception>
        private static int GetPort(string p)
        {
            int port;

            if (!Int32.TryParse(p, out port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(String.Format("Invalid end point port '{0}'", p));
            }

            return port;
        }

        /// <summary>
        /// Gets the I pfrom host.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>IPAddress.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        private static async Task<IPAddress> GetIPfromHost(string p)
        {
            var hosts = await Dns.GetHostAddressesAsync(p).ConfigureAwait(false);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(String.Format("Host not found: {0}", p));

            return hosts[0];
        }

        public IpAddressInfo ParseIpAddress(string ipAddress)
        {
            IpAddressInfo info;
            if (TryParseIpAddress(ipAddress, out info))
            {
                return info;
            }

            throw new ArgumentException("Invalid ip address: " + ipAddress);
        }

        public bool TryParseIpAddress(string ipAddress, out IpAddressInfo ipAddressInfo)
        {
            IPAddress address;
            if (IPAddress.TryParse(ipAddress, out address))
            {
                ipAddressInfo = ToIpAddressInfo(address);
                return true;
            }

            ipAddressInfo = null;
            return false;
        }

        public static IpEndPointInfo ToIpEndPointInfo(IPEndPoint endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }

            return new IpEndPointInfo(ToIpAddressInfo(endpoint.Address), endpoint.Port);
        }

        public static IPEndPoint ToIPEndPoint(IpEndPointInfo endpoint)
        {
            if (endpoint == null)
            {
                return null;
            }

            return new IPEndPoint(ToIPAddress(endpoint.IpAddress), endpoint.Port);
        }

        public static IPAddress ToIPAddress(IpAddressInfo address)
        {
            if (address.Equals(IpAddressInfo.Any))
            {
                return IPAddress.Any;
            }
            if (address.Equals(IpAddressInfo.IPv6Any))
            {
                return IPAddress.IPv6Any;
            }
            if (address.Equals(IpAddressInfo.Loopback))
            {
                return IPAddress.Loopback;
            }
            if (address.Equals(IpAddressInfo.IPv6Loopback))
            {
                return IPAddress.IPv6Loopback;
            }

            return IPAddress.Parse(address.Address);
        }

        public static IpAddressInfo ToIpAddressInfo(IPAddress address)
        {
            if (address.Equals(IPAddress.Any))
            {
                return IpAddressInfo.Any;
            }
            if (address.Equals(IPAddress.IPv6Any))
            {
                return IpAddressInfo.IPv6Any;
            }
            if (address.Equals(IPAddress.Loopback))
            {
                return IpAddressInfo.Loopback;
            }
            if (address.Equals(IPAddress.IPv6Loopback))
            {
                return IpAddressInfo.IPv6Loopback;
            }
            return new IpAddressInfo(address.ToString(), address.AddressFamily == AddressFamily.InterNetworkV6 ? IpAddressFamily.InterNetworkV6 : IpAddressFamily.InterNetwork);
        }

        public async Task<IpAddressInfo[]> GetHostAddressesAsync(string host)
        {
            var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            return addresses.Select(ToIpAddressInfo).ToArray();
        }

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        public virtual IEnumerable<NetworkShare> GetNetworkShares(string path)
        {
            return new List<NetworkShare>();
        }

        /// <summary>
        /// Gets available devices within the domain
        /// </summary>
        /// <returns>PC's in the Domain</returns>
        public virtual IEnumerable<FileSystemEntryInfo> GetNetworkDevices()
        {
            return new List<FileSystemEntryInfo>();
        }
    }
}
