#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jellyfin.Networking.Structures
{
    /// <summary>
    /// Object that holds a host name.
    /// </summary>
    public class IPHost : IPObject
    {
        /// <summary>
        /// Represents an IPHost that has no value.
        /// </summary>
        public static readonly IPHost None = new IPHost(string.Empty, IPAddress.None);

        /// <summary>
        /// Time when last resolved.
        /// </summary>
        private long _lastResolved;

        /// <summary>
        /// Gets the IP Addresses, attempting to resolve the name, if there are none.
        /// </summary>
        private IPAddress[] _addresses;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        public IPHost(string name)
        {
            HostName = name ?? throw new ArgumentNullException(nameof(name));
            _addresses = Array.Empty<IPAddress>();
            Resolved = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        /// <param name="address">Address to assign.</param>
        private IPHost(string name, IPAddress address)
        {
            HostName = name ?? throw new ArgumentNullException(nameof(name));
            _addresses = new IPAddress[] { address ?? throw new ArgumentNullException(nameof(address)) };
            Resolved = !address.Equals(IPAddress.None);
        }

        /// <summary>
        /// Gets or sets the object's first IP address.
        /// </summary>
        public override IPAddress Address
        {
            get
            {
                if (_addresses.Length == 0)
                {
                    ResolveHost();
                }

                return _addresses.Length > 0 ? this[0] : IPAddress.None;
            }

            set
            {
                // Do nothing - as this object cannot set the address this way.
            }
        }

        /// <summary>
        /// Gets or sets the object's first IP's subnet prefix.
        /// The setter does nothing, but shouldn't raise an exception.
        /// </summary>
        public override byte PrefixLength
        {
            get
            {
                if (_addresses.Length == 0)
                {
                    ResolveHost();
                }

                return (byte)((_addresses.Length > 0) ? 128 : 0);
            }

            set
            {
                // Not implemented.
            }
        }

        /// <summary>
        /// Gets or sets timeout value before resolve required, in minutes.
        /// </summary>
        public byte Timeout { get; set; } = 30;

        /// <summary>
        /// Gets a value indicating whether the address has a value.
        /// </summary>
        public bool HasAddress
        {
            get
            {
                return _addresses.Length > 0;
            }
        }

        /// <inheritdoc/>
        public override IPObject NetworkAddress => new IPNetAddress(this[0]);

        /// <summary>
        /// Gets the host name of this object.
        /// </summary>
        public string HostName { get; }

        /// <summary>
        /// Gets a value indicating whether this host has attempted to be resolved.
        /// </summary>
        public bool Resolved { get; private set; }

        /// <summary>
        /// Gets or sets the IP Addresses associated with this object.
        /// </summary>
        /// <param name="index">Index of address.</param>
        public IPAddress this[int index]
        {
            get
            {
                if (_addresses.Length == 0)
                {
                    ResolveHost();
                }

                return index >= 0 && index < _addresses.Length ? _addresses[index] : IPAddress.None;
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
                if (IPAddress.TryParse(host, out IPAddress hostIP))
                {
                    hostObj = new IPHost(host, hostIP);
                    return true;
                }

                // See if it's an IPv6 with port address e.g. [::1]:120.
                int i = host.IndexOf("]:", StringComparison.OrdinalIgnoreCase);
                if (i != -1)
                {
                    return TryParse(host.Remove(i - 1).TrimStart(' ', '['), out hostObj);
                }
                else
                {
                    // See if it's an IPv6 in [] with no port.
                    i = host.IndexOf("]", StringComparison.OrdinalIgnoreCase);
                    if (i != -1)
                    {
                        return TryParse(host.Remove(i - 1).TrimStart(' ', '['), out hostObj);
                    }

                    // Is it a host or IPv4 with port?
                    string[] hosts = host.Split(':');

                    if (hosts.Length > 2)
                    {
                        hostObj = new IPHost(string.Empty, IPAddress.None);
                        return false;
                    }

                    // Remove port from IPv4 if it exists.
                    host = hosts[0];

                    if (string.Equals("localhost", host, StringComparison.OrdinalIgnoreCase))
                    {
                        hostObj = new IPHost(host, new IPAddress(Ipv4Loopback));
                        return true;
                    }

                    if (IPNetAddress.TryParse(host, out IPNetAddress netIP))
                    {
                        // Host name is an ip address, so fake resolve.
                        hostObj = new IPHost(host, netIP.Address);
                        return true;
                    }
                }

                // Only thing left is to see if it's a host string.
                if (!string.IsNullOrEmpty(host))
                {
                    // Use regular expression as CheckHostName isn't RFC5892 compliant.
                    // Modified from gSkinner's expression at https://stackoverflow.com/questions/11809631/fully-qualified-domain-name-validation
                    Regex re = new Regex(@"^(?!:\/\/)(?=.{1,255}$)((.{1,63}\.){0,127}(?![0-9]*$)[a-z0-9-]+\.?)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (re.Match(host).Success)
                    {
                        hostObj = new IPHost(host);
                        return true;
                    }
                }
            }

            hostObj = IPHost.None;
            return false;
        }

        /// <summary>
        /// Attempts to parse the host string.
        /// </summary>
        /// <param name="host">Host name to parse.</param>
        /// <returns>Object representing the string, if it has successfully been parsed.</returns>
        public static IPHost Parse(string host)
        {
            if (!string.IsNullOrEmpty(host) && IPHost.TryParse(host, out IPHost res))
            {
                return res;
            }

            throw new InvalidCastException("Host does not contain a valid value. {host}");
        }

        /// <summary>
        /// Returns the Addresses that this item resolved to.
        /// </summary>
        /// <returns>IPAddress Array.</returns>
        public IPAddress[] GetAddresses()
        {
            if (_addresses.Length == 0)
            {
                ResolveHost();
            }

            return _addresses;
        }

        /// <inheritdoc/>
        public override bool Equals(IPObject? other)
        {
            if (other is IPHost otherObj)
            {
                // Do we have the name Hostname?
                if (string.Equals(otherObj.HostName, HostName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (_addresses.Length == 0)
                {
                    ResolveHost();
                }

                // Do any of our IP addresses match?
                foreach (IPAddress a in _addresses)
                {
                    if (otherObj.Exists(a))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Exists(IPAddress address)
        {
            if (address != null)
            {
                if (_addresses.Length == 0 && !ResolveHost())
                {
                    return false;
                }

                // If ResolvedHostInternal returns true, Addresses is non null.
                foreach (IPAddress addr in _addresses)
                {
                    if (addr.Equals(address))
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
            if (_addresses.Length == 0)
            {
                ResolveHost();
            }

            // Returns true if interfaces are only IP6.
            if (_addresses.Length > 0)
            {
                foreach (IPAddress i in _addresses)
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
        public override string ToString()
        {
            // StringBuilder not optimum here.
            string output = string.Empty;
            if (_addresses.Length > 0)
            {
                if (_addresses.Length > 1)
                {
                    output = "[";
                }

                foreach (var i in _addresses)
                {
                    output += $"{i}/32,";
                }

                output = output[0..^1]; // output = output.Remove(output.Length - 1);

                if (_addresses.Length > 1)
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

        /// <inheritdoc/>
        public override void Remove(AddressFamily family)
        {
            if (_addresses.Length == 0)
            {
                ResolveHost();
            }

            if (_addresses.Length > 0)
            {
                List<IPAddress> add = new List<IPAddress>();

                // Filter out IP6 addresses.
                foreach (IPAddress addr in _addresses)
                {
                    if (addr.AddressFamily == family)
                    {
                        continue;
                    }

                    add.Add(addr);
                }

                _addresses = add.ToArray();
            }
        }

        /// <summary>
        /// Attempt to resolve the ip address of a host.
        /// </summary>
        /// <returns>The result of the comparison function.</returns>
        private bool ResolveHost()
        {
            // When was the last time we resolved?
            if (_lastResolved == 0)
            {
                _lastResolved = DateTime.Now.Ticks;
            }

            // If we haven't resolved before, or out timer has run out...
            if ((_addresses.Length == 0 && !Resolved) || (TimeSpan.FromTicks(DateTime.Now.Ticks - _lastResolved).TotalMinutes > Timeout))
            {
                _lastResolved = DateTime.Now.Ticks;
                ResolveHostInternal().GetAwaiter().GetResult();
                Resolved = true;
            }

            return _addresses.Length > 0;
        }

        /// <summary>
        /// Task that looks up a Host name and returns its IP addresses.
        /// </summary>
        /// <returns>Array of IPAddress objects.</returns>
        private async Task ResolveHostInternal()
        {
            if (!string.IsNullOrEmpty(HostName))
            {
                // Resolves the host name - so save a DNS lookup.
                if (string.Equals(HostName, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    // Defer to IPv4 first.
                    _addresses = new IPAddress[] { new IPAddress(Ipv4Loopback) };
                }

                if (Uri.CheckHostName(HostName).Equals(UriHostNameType.Dns))
                {
                    try
                    {
                        IPHostEntry ip = await Dns.GetHostEntryAsync(HostName).ConfigureAwait(false);
                        _addresses = ip.AddressList;
                    }
                    catch (SocketException)
                    {
                        // Ignore socket errors, as the result value will just be an empty array.
                    }
                }
            }
        }
    }
}
