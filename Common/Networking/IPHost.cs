using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Networking
{
    /// <summary>
    /// Object that holds a host name.
    /// </summary>
    public class IPHost : IPObject
    {
        private string _hostName = string.Empty;

        /// <summary>
        /// Gets the IP Addresses, attempting to resolve the name, if there are none.
        /// </summary>
        private IPAddress[] _addresses;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class without any checking if the host name is valid.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        public IPHost(string name) => _hostName = name;

        /// <summary>
        /// Gets the host name of this object.
        /// </summary>
        public string HostName { get => _hostName;  }

        /// <summary>
        /// Gets or sets the IP Addresses associated with this object.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public IPAddress[] Addresses
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                if (_addresses == null)
                {
                    ResolveHostInternal();
                }

                return _addresses;
            }

            set
            {
                _addresses = value;
            }
        }

        /// <summary>
        /// Attempts to parse the host string.
        /// </summary>
        /// <param name="host">Host name to parse.</param>
        /// <param name="hostObj">Object representing the string, if it has successfully been parsed.</param>
        /// <returns>Success result of the parsing.</returns>
        public static bool TryParse(string host, out IPHost hostObj)
        {
            if (!string.IsNullOrEmpty(host))
            {
                int i = host.IndexOf("]", StringComparison.OrdinalIgnoreCase);
                if (i != -1)
                {
                    // Assume host is encased in [ ] and is IPv6 address.
                    host = host.TrimStart()[1..i];
                }
                else
                {
                    // Remove port from IPv4 if it exists.
                    host = host.Split(':')[0];
                }

                if (!string.IsNullOrEmpty(host))
                {
                    if (Uri.CheckHostName(host).Equals(UriHostNameType.Dns))
                    {
                        hostObj = new IPHost(host);
                        return true;
                    }

                    if (IPAddress.TryParse(host, out IPAddress ip))
                    {
                        // Host name is an ip address, so fake resolve.
                        hostObj = new IPHost(host)
                        {
                            _addresses = new IPAddress[] { ip }
                        };

                        return true;
                    }
                }
            }

            hostObj = null;
            return false;
        }

        /// <summary>
        /// Attempts to parse the host string.
        /// </summary>
        /// <param name="host">Host name to parse.</param>
        /// <returns>Object representing the string, if it has successfully been parsed.</returns>
        public static IPHost Parse(string host)
        {
            if (!string.IsNullOrEmpty(host))
            {
                if (IPHost.TryParse(host, out IPHost res))
                {
                    return res;
                }
            }

            throw new InvalidCastException("String is not a value host name.");
        }

        /// <summary>
        /// Task that looks up a Host name and returns its IP addresses.
        /// </summary>
        /// <param name="host">Host name to perform a DNS loopup on.</param>
        /// <returns>Array of IPAddress objects.</returns>
        public static async Task<IPAddress[]> Resolve(string host)
        {
            if (!string.IsNullOrEmpty(host))
            {
                // Resolves the host name - so save a DNS lookup.
                if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    // Defer to IPv4 first.
                    return new IPAddress[] { new IPAddress(new byte[] { 127, 0, 0, 1 }) };
                }

                if (Uri.CheckHostName(host).Equals(UriHostNameType.Dns))
                {
                    try
                    {
                        IPHostEntry ip = await Dns.GetHostEntryAsync(host).ConfigureAwait(false);
                        return ip.AddressList;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        // Ignore errors, as the result value will just be null.
                    }
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Copy(IPObject ip)
        {
            if (ip is IPHost ipObj)
            {
                _hostName = ipObj.HostName;
                _addresses = ipObj.Addresses;
            }
            else
            {
                throw new InvalidCastException("Parameter is not an IPHost.");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(IPObject ip)
        {
            if (ip is IPHost ipObj)
            {
                return string.Equals(ipObj.HostName, HostName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <inheritdoc/>

        /// <inheritdoc/>
        public override bool Exists(IPAddress ip)
        {
            if (ip != null)
            {
                if (Addresses == null)
                {
                    if (!ResolveHostInternal())
                    {
                        return false;
                    }
                }

                foreach (IPAddress addr in Addresses)
                {
                    if (addr.Equals(ip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool IsIP6()
        {
            // Returns true if interfaces are only IP6.
            if (Addresses != null)
            {
                foreach (IPAddress i in Addresses)
                {
                    if (i.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool IsAIPIPA()
        {
            // Returns true if interfaces are only AIPIPA.
            if (Addresses != null)
            {
                foreach (IPAddress i in Addresses)
                {
                    if (!IsAIPIPA(i))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string output = $"{HostName}[";
            if (Addresses != null)
            {
                foreach (IPAddress a in Addresses)
                {
                    output += $"{a} ";
                }
            }

            return output.Trim() + "]";
        }

        /// <inheritdoc/>
        protected override IPAddress GetAddressInternal()
        {
            if (Addresses != null && Addresses.Length > 0)
            {
                return Addresses[0];
            }

            return null;
        }

        /// <summary>
        /// Attempt to resolve the ip address of a host.
        /// </summary>
        /// <returns>The result of the comparison function.</returns>
        private bool ResolveHostInternal()
        {
            _addresses = Resolve(HostName).Result;
            return _addresses != null;
        }
    }
}
