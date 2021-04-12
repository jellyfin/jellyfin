#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Object that holds a host name.
    /// </summary>
    public class IPHost : IPNetAddress
    {
        /// <summary>
        /// Gets or sets timeout value before resolve required, in minutes.
        /// </summary>
        public const int Timeout = 30;

        /// <summary>
        /// Time when last resolved in ticks.
        /// </summary>
        private DateTime? _lastResolved;

        /// <summary>
        /// Gets the IP Addresses, attempting to resolve the name, if there are none.
        /// </summary>
        private IPAddress[] _addresses;

        private string _hostName;

        private IpClassType _ipType = IpClassType.IpBoth;

#pragma warning disable CS8618 // Reason: _hostName is set via HostName property.
        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        public IPHost(string name)
        {
            HostName = name ?? throw new ArgumentNullException(nameof(name));
            _addresses = Array.Empty<IPAddress>();
            Resolved = false;
            _lastResolved = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPHost"/> class.
        /// </summary>
        /// <param name="name">Host name to assign.</param>
        /// <param name="address">Address to assign.</param>
        private IPHost(string name, IPAddress address)
        {
            HostName = name ?? throw new ArgumentNullException(nameof(name));
            Resolved = true;
            _lastResolved = DateTime.UtcNow;
            _addresses = new IPAddress[] { address ?? throw new ArgumentNullException(nameof(address)) };
        }
#pragma warning restore CS8618

        /// <summary>
        /// Gets the object's first IP address.
        /// </summary>
        public override IPAddress Address
        {
            get
            {
                return this[0];
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
                ResolveHost();
                return Address.AddressFamily switch
                {
                    AddressFamily.InterNetwork => 32,
                    AddressFamily.InterNetworkV6 => 128,
                    _ => 255
                };
            }

            set
            {
                // Not implemented, as a host object can only have a prefix length of 128 (IPv6) or 32 (IPv4) prefix length,
                // which is automatically determined by it's IP type. Anything else is meaningless.
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the address has a value.
        /// </summary>
        public bool HasAddress => _addresses.Length != 0;

        /// <summary>
        /// Gets the host name of this object.
        /// </summary>
        public string HostName
        {
            get => _hostName;

            private set
            {
                char[] separators = { '/', '%' };
                var i = value.IndexOfAny(separators);

                if (i != -1)
                {
                    _hostName = value.Substring(0, i);
                }
                else
                {
                    _hostName = value;
                }
            }
        }

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
                ResolveHost();
                return _addresses[index];
            }
        }

        /// <summary>
        /// Attempts to parse the host string.
        /// </summary>
        /// <param name="host">Host name to parse.</param>
        /// <param name="hostObj">Object representing the string, if it has successfully been parsed.</param>
        /// <param name="ipType"><see cref="IpClassType"/> to filter on.</param>
        /// <returns><c>true</c> if the parsing is successful, <c>false</c> if not.</returns>
        public static bool TryParse(string host, [NotNullWhen(true)] out IPHost? hostObj, IpClassType ipType)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                hostObj = null;
                return false;
            }

            host = host.Trim();

            // See if it's an IPv6 with port address e.g. [::1] or [::1]:120.
            if (host[0] == '[')
            {
                int i = host.IndexOf(']', StringComparison.Ordinal);
                if (i != -1)
                {
                    return TryParse(host.Remove(i)[1..], out hostObj, ipType);
                }

                hostObj = null;
                return false;
            }

            string[] hosts;

            // Use regular expression as CheckHostName isn't RFC5892 compliant.
            // Modified from gSkinner's expression at https://stackoverflow.com/questions/11809631/fully-qualified-domain-name-validation
            string pattern = @"(?im)^(?!:\/\/)(?=.{1,255}$)((.{1,63}\.){0,127}(?![0-9]*$)[a-z0-9-]+\.?)(:(\d){1,5}){0,1}$";

            hosts = host.Split(':');

            if (hosts.Length <= 2)
            {
                // Is hostname or hostname:port
                if (Regex.IsMatch(hosts[0], pattern))
                {
                    hostObj = new IPHost(hosts[0])
                    {
                        _ipType = ipType
                    };
                    return true;
                }

                // Is an IP4 or IP4:port
                host = hosts[0];

                if (IPAddress.TryParse(host, out var netAddress))
                {
                    if (((netAddress.AddressFamily == AddressFamily.InterNetwork) && ipType == IpClassType.Ip6Only) ||
                        ((netAddress.AddressFamily == AddressFamily.InterNetworkV6) && ipType == IpClassType.Ip4Only))
                    {
                        hostObj = null;
                        return false;
                    }

                    // Host name is an ip4 address, so fake resolve.
                    hostObj = new IPHost(host, netAddress);
                    return true;
                }
            }
            else if (hosts.Length <= 9 && IPAddress.TryParse(host, out var netAddress)) // 8 octets + port
            {
                if (((netAddress.AddressFamily == AddressFamily.InterNetwork) && ipType == IpClassType.Ip6Only) ||
                    ((netAddress.AddressFamily == AddressFamily.InterNetworkV6) && ipType == IpClassType.Ip4Only))
                {
                    hostObj = null;
                    return false;
                }

                // Host name is an ip6 address, so fake resolve.
                hostObj = new IPHost(host, netAddress);
                return true;
            }

            hostObj = null;
            return false;
        }

        /// <summary>
        /// Attempts to parse the host string.
        /// </summary>
        /// <param name="host">Host name to parse.</param>
        /// <returns>Object representing the string, if it has successfully been parsed.</returns>
        public static new IPHost Parse(string host)
        {
            if (!string.IsNullOrEmpty(host) && IPHost.TryParse(host, out IPHost? res, IpClassType.IpBoth))
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
            ResolveHost();
            return _addresses;
        }

        /// <inheritdoc/>
        public override bool Contains(IPAddress address)
        {
            if (address != null && Address != null)
            {
                if (address.IsIPv4MappedToIPv6)
                {
                    address = address.MapToIPv4();
                }

                foreach (var addr in GetAddresses())
                {
                    if (address.Equals(addr))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPNetAddress other)
        {
            if (other is IPHost otherObj)
            {
                // Do we have the name Hostname?
                if (string.Equals(otherObj.HostName, HostName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!ResolveHost() || !otherObj.ResolveHost())
                {
                    return false;
                }

                // Do any of our IP addresses match?
                foreach (IPAddress addr in _addresses)
                {
                    foreach (IPAddress otherAddress in otherObj._addresses)
                    {
                        if (addr.Equals(otherAddress))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool IsIP6()
        {
            // Returns true if interfaces are only IP6.
            if (ResolveHost())
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
            if (GetAddresses().Length > 0)
            {
                string output = HostName + " [";
                foreach (var i in _addresses)
                {
                    if (i.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (i.Equals(IPAddress.Any))
                        {
                            output += "Any IP4 Address,";
                        }
                        else
                        {
                            output += $"{i}/32,";
                        }
                    }
                    else if (i.Equals(IPAddress.IPv6Any))
                    {
                        output += "Any IP6 Address,";
                    }
                    else
                    {
                        output += $"{i}/128,";
                    }
                }

                return output[0..^1] + ']';
            }

            return string.IsNullOrEmpty(HostName) ? "None" : HostName;
        }

        /// <inheritdoc/>
        public override bool Contains(IPNetAddress ip)
        {
            // An IPHost cannot contain another IPNetAddress, it can only be equal.
            return Equals(ip);
        }

        /// <summary>
        /// Attempt to resolve the ip address of a host.
        /// </summary>
        /// <returns><c>true</c> if any addresses have been resolved, otherwise <c>false</c>.</returns>
        private bool ResolveHost()
        {
            // When was the last time we resolved?
            if (_lastResolved == null)
            {
                _lastResolved = DateTime.UtcNow;
            }

            // If we haven't resolved before, or our timer has run out...
            if ((_addresses.Length == 0 && !Resolved) || (DateTime.UtcNow > _lastResolved.Value.AddMinutes(Timeout)))
            {
                _lastResolved = DateTime.UtcNow;
                if (ResolveHostInternal().GetAwaiter().GetResult())
                {
                    Resolved = true;
                    if (_ipType == IpClassType.Ip4Only)
                    {
                        _addresses = _addresses.Where(p => p.AddressFamily == AddressFamily.InterNetwork).ToArray();
                    }
                    else if (_ipType == IpClassType.Ip6Only)
                    {
                        _addresses = _addresses.Where(p => p.AddressFamily == AddressFamily.InterNetworkV6).ToArray();
                    }
                }
            }

            return _addresses.Length > 0;
        }

        /// <summary>
        /// Task that looks up a Host name and returns its IP addresses.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> ResolveHostInternal()
        {
            if (!string.IsNullOrEmpty(HostName))
            {
                // Resolves the host name - so save a DNS lookup.
                if (string.Equals(HostName, "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    _addresses = new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback };
                    return true;
                }

                if (Uri.CheckHostName(HostName).Equals(UriHostNameType.Dns))
                {
                    try
                    {
                        IPHostEntry ip = await Dns.GetHostEntryAsync(HostName).ConfigureAwait(false);
                        _addresses = ip.AddressList;
                        return true;
                    }
                    catch (SocketException ex)
                    {
                        // Log and then ignore socket errors, as the result value will just be an empty array.
                        Debug.WriteLine("GetHostEntryAsync failed with {Message}.", ex.Message);
                    }
                }
            }

            return false;
        }
    }
}
