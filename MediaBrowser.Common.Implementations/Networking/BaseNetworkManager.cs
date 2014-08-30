using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MediaBrowser.Common.Implementations.Networking
{
    public abstract class BaseNetworkManager
    {
        protected ILogger Logger { get; private set; }

        protected BaseNetworkManager(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
        public IEnumerable<string> GetLocalIpAddresses()
        {
            var list = GetIPsDefault().Where(i => !IPAddress.IsLoopback(i)).Select(i => i.ToString()).ToList();

            if (list.Count > 0)
            {
                return list;
            }

            return GetLocalIpAddressesFallback();
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

            const int lengthMatch = 4;

            if (endpoint.Length >= lengthMatch)
            {
                var prefix = endpoint.Substring(0, lengthMatch);

                if (GetLocalIpAddresses()
                    .Any(i => i.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            // Private address space:
            // http://en.wikipedia.org/wiki/Private_network

            var isPrivate =

                // If url was requested with computer name, we may see this
                endpoint.IndexOf("::", StringComparison.OrdinalIgnoreCase) != -1 ||

                endpoint.StartsWith("10.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("192.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("172.", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("169.", StringComparison.OrdinalIgnoreCase);

            if (isPrivate)
            {
                return true;
            }

            IPAddress address;
            if (resolveHost && !IPAddress.TryParse(endpoint, out address))
            {
                var host = new Uri(endpoint).DnsSafeHost;

                Logger.Debug("Resolving host {0}", host);

                try
                {
                    address = GetIpAddresses(host).FirstOrDefault();

                    if (address != null)
                    {
                        Logger.Debug("{0} resolved to {1}", host, address);

                        return IsInLocalNetworkInternal(address.ToString(), false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error resovling hostname {0}", ex, host);
                }
            }

            return false;
        }
        
        public IEnumerable<IPAddress> GetIpAddresses(string hostName)
        {
            return Dns.GetHostAddresses(hostName);
        }

        private IEnumerable<IPAddress> GetIPsDefault()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                var props = adapter.GetIPProperties();
                var gateways = from ga in props.GatewayAddresses
                               where !ga.Address.Equals(IPAddress.Any)
                               select true;

                if (!gateways.Any())
                {
                    continue;
                }

                foreach (var uni in props.UnicastAddresses)
                {
                    var address = uni.Address;
                    if (address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }
                    yield return address;
                }
            }
        }

        private IEnumerable<string> GetLocalIpAddressesFallback()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            // Reverse them because the last one is usually the correct one
            // It's not fool-proof so ultimately the consumer will have to examine them and decide
            return host.AddressList
                .Where(i => i.AddressFamily == AddressFamily.InterNetwork)
                .Select(i => i.ToString())
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
