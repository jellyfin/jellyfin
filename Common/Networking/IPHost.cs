namespace Common.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Object that holds a host name.
    /// </summary>
    public class IPHost : IPObject
    {
        /// <summary>
        /// Time when last resolved. Timeout is 30 minutes..
        /// </summary>
        private long _lastResolved;

        /// <summary>
        /// Host name of this object..
        /// </summary>
        private string _hostName = string.Empty;

        /// <summary>
        /// Gets the IP Addresses, attempting to resolve the name, if there are none..
        /// </summary>
        private IPAddress[] _addresses;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        public IPHost(string name)
        {
            _hostName = name;
            Valid = true;
        }

        /// <summary>
        /// Gets the host name of this object..
        /// </summary>
        public string HostName { get => _hostName; }

        /// <summary>
        /// Gets a value indicating whether this object is valid.
        /// </summary>
        public bool Valid { get; internal set; }

        /// <summary>
        /// Gets or sets the IP Addresses associated with this object..
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays.
        public IPAddress[] Addresses
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                if (_addresses == null)
                {
                    _ = ResolveHostInternal();
                }

                return _addresses;
            }

            set
            {
                _addresses = value;
                _lastResolved = 0;
                Valid = true;
                if (_addresses != null)
                {
                    if (_addresses.Length == 1)
                    {
                        AddressFamily = _addresses[0].AddressFamily;
                    }
                    else
                    {
                        AddressFamily = AddressFamily.Unspecified;
                    }
                }
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
                    // Does it have a port at the end?
                    string[] hosts = host.Split(':');

                    if (hosts.Length > 2)
                    {
                        // Could be an abreviated IP6 address.
                        if (IPAddress.TryParse(host, out IPAddress ip))
                        {
                            // Host name is an ip address, so fake resolve.
                            hostObj = new IPHost(host)
                            {
                                _addresses = new IPAddress[] { ip }
                            };

                            return true;
                        }

                        hostObj = null;
                        return false;
                    }

                    // Remove port from IPv4 if it exists.
                    host = hosts[0];
                }

                if (!string.IsNullOrEmpty(host))
                {
                    // Use regular expression as CheckHostName isn't RFC5892 compliant.
                    // Modified from gSkinner's expression at https://stackoverflow.com/questions/11809631/fully-qualified-domain-name-validation
                    Regex re = new Regex(@"^(?!:\/\/)(?=.{1,255}$)((.{1,63}\.){0,127}(?![0-9]*$)[a-z0-9-]+\.?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (re.Match(host).Success)
                    // if (Uri.CheckHostName(host) != UriHostNameType.Unknown)
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
                    catch
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
            string output = string.Empty;
            if (Addresses != null)
            {
                if (Addresses.Length > 1)
                {
                    output = "[";
                }

                foreach (var i in Addresses)
                {
                    output += $"{i}/32,";
                }

                output = output[0..^1];

                if (Addresses.Length > 1)
                {
                    output += "]";
                }
            }
            else
            {
                output = HostName;
            }

            return output;
        }

        /// <summary>
        /// Removes IP6 addresses from this object.
        /// </summary>
        public override void RemoveIP6()
        {
            if (Addresses != null)
            {
                List<IPAddress> add = new List<IPAddress>();

                // Filter out IP6 addresses
                foreach (IPAddress addr in _addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        continue;
                    }

                    add.Add(addr);
                }

                _addresses = add.ToArray();
            }
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
            // When was the last time we resolved?
            if (_lastResolved == 0)
            {
                _lastResolved = DateTime.Now.Ticks;
            }

            // If we haven't resolved before, or out timer has run out...
            if ((_addresses == null && Valid) || (TimeSpan.FromTicks(DateTime.Now.Ticks - _lastResolved).TotalMinutes > 30))
            {
                _lastResolved = DateTime.Now.Ticks;
                _addresses = Resolve(HostName).Result;
                Valid = _addresses != null;
            }

            return Valid;
        }
    }
}
