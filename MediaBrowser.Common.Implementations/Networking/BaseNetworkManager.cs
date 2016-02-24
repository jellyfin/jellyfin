using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MoreLinq;

namespace MediaBrowser.Common.Implementations.Networking
{
    public abstract class BaseNetworkManager
    {
        protected ILogger Logger { get; private set; }
        private DateTime _lastRefresh;

        protected BaseNetworkManager(ILogger logger)
        {
            Logger = logger;
        }

		private List<IPAddress> _localIpAddresses;
        private readonly object _localIpAddressSyncLock = new object();

        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
		public IEnumerable<IPAddress> GetLocalIpAddresses()
        {
            const int cacheMinutes = 5;

            lock (_localIpAddressSyncLock)
            {
                var forceRefresh = (DateTime.UtcNow - _lastRefresh).TotalMinutes >= cacheMinutes;

                if (_localIpAddresses == null || forceRefresh)
                {
                    var addresses = GetLocalIpAddressesInternal().ToList();

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
				list.AddRange(GetLocalIpAddressesFallback());
            }

			return list.Where(FilterIpAddress).DistinctBy(i => i.ToString());
        }

		private bool FilterIpAddress(IPAddress address)
        {
			var addressString = address.ToString ();

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

                        address = GetIpAddresses(host).FirstOrDefault();

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

        public IEnumerable<IPAddress> GetIpAddresses(string hostName)
        {
            return Dns.GetHostAddresses(hostName);
        }

		private List<IPAddress> GetIPsDefault()
		{
			NetworkInterface[] interfaces;

			try
			{
				interfaces = NetworkInterface.GetAllNetworkInterfaces();
			}
			catch (Exception ex)
			{
				Logger.ErrorException("Error in GetAllNetworkInterfaces", ex);
				return new List<IPAddress>();
			}

			return interfaces.SelectMany(network => {

				try
				{
                    Logger.Debug("Querying interface: {0}. Type: {1}. Status: {2}", network.Name, network.NetworkInterfaceType, network.OperationalStatus);

					var properties = network.GetIPProperties();

					return properties.UnicastAddresses
                        .Where(i => i.IsDnsEligible)
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

		private IEnumerable<IPAddress> GetLocalIpAddressesFallback()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

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
        public int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
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
            return Parse(endpointstring, -1);
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <param name="defaultport">The defaultport.</param>
        /// <returns>IPEndPoint.</returns>
        /// <exception cref="System.ArgumentException">Endpoint descriptor may not be empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        private static IPEndPoint Parse(string endpointstring, int defaultport)
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
                    ipaddy = GetIPfromHost(values[0]);
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
        private static IPAddress GetIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(String.Format("Host not found: {0}", p));

            return hosts[0];
        }
    }
}
