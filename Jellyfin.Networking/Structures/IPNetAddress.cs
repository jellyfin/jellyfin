#nullable enable

using System;
using System.Net;
using System.Net.Sockets;

namespace Jellyfin.Networking.Structures
{
    /// <summary>
    /// An object that holds and IP address and subnet mask.
    /// </summary>
    public class IPNetAddress : IPObject
    {
        /// <summary>
        /// Represents an IPNetAddress that has no value.
        /// </summary>
        public static readonly IPNetAddress None = new IPNetAddress(IPAddress.None);

        /// <summary>
        /// IPv4 multicast address.
        /// </summary>
        public static readonly IPAddress MulticastIPv4 = IPAddress.Parse("239.255.255.250");

        /// <summary>
        /// IPv6 local link multicast address.
        /// </summary>
        public static readonly IPAddress MulticastIPv6LinkLocal = IPAddress.Parse("ff02::C");

        /// <summary>
        /// IPv6 site local multicast address.
        /// </summary>
        public static readonly IPAddress MulticastIPv6SiteLocal = IPAddress.Parse("ff05::C");

        /// <summary>
        /// IP4Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP4Loopback = IPNetAddress.Parse("127.0.0.1/32");

        /// <summary>
        /// IP6Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP6Loopback = IPNetAddress.Parse("::1");

        /// <summary>
        /// Object's IP address.
        /// </summary>
        private IPAddress _address;

        /// <summary>
        /// Object's network address.
        /// </summary>
        private IPNetAddress? _networkAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        public IPNetAddress(IPAddress address)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            PrefixLength = (byte)(address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        /// <param name="subnet">Mask to assign.</param>
        public IPNetAddress(IPAddress address, IPAddress subnet)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            if (subnet == null)
            {
                throw new ArgumentNullException(nameof(subnet));
            }

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("This method of creation is only for IPv4 addresses.");
            }

            PrefixLength = MaskToCidr(subnet);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">IP Address.</param>
        /// <param name="prefixLength">Mask as a CIDR.</param>
        public IPNetAddress(IPAddress address, byte prefixLength)
        {
            _address = address ?? throw new ArgumentNullException(nameof(address));
            PrefixLength = prefixLength;
        }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public override IPAddress Address
        {
            get
            {
                return _address;
            }

            set
            {
                _address = value ?? IPAddress.None;
            }
        }

        /// <inheritdoc/>
        public override IPObject NetworkAddress
        {
            get
            {
                if (_networkAddress == null)
                {
                    var value = NetworkAddressOf(_address, PrefixLength);
                    _networkAddress = new IPNetAddress(value.Item1, value.Item2);
                }

                return _networkAddress;
            }
        }

        /// <inheritdoc/>
        public override byte PrefixLength { get; set; }

        /// <summary>
        /// Gets the subnet mask of this object.
        /// </summary>
        public IPAddress Mask
        {
            get
            {
                if (!_address.Equals(IPAddress.None))
                {
                    return CidrToMask(PrefixLength, _address.AddressFamily);
                }

                return IPAddress.None;
            }
        }

        /// <summary>
        /// Try to parse the address and subnet strings into an IPNetAddress object.
        /// </summary>
        /// <param name="addr">IP address to parse. Can be CIDR or X.X.X.X notation.</param>
        /// <param name="ip">Resultant object.</param>
        /// <returns>True if the values parsed successfully. False if not, resulting in the IP being null.</returns>
        public static bool TryParse(string addr, out IPNetAddress ip)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                addr = addr.Trim();

                // Try to parse it as is.
                if (IPAddress.TryParse(addr, out IPAddress res))
                {
                    ip = new IPNetAddress(res);
                    return true;
                }

                // Is it a network?
                string[] tokens = addr.Split("/");

                if (tokens.Length == 2)
                {
                    tokens[0] = tokens[0].TrimEnd();
                    tokens[1] = tokens[1].TrimStart();

                    if (IPAddress.TryParse(tokens[0], out res))
                    {
                        if (byte.TryParse(tokens[1], out byte cidr))
                        {
                            ip = new IPNetAddress(res, cidr);
                            return true;
                        }

                        if (IPAddress.TryParse(tokens[1], out IPAddress mask))
                        {
                            ip = new IPNetAddress(res, mask);
                            return true;
                        }
                    }
                }
            }

            ip = IPNetAddress.None;
            return false;
        }

        /// <summary>
        /// Parses the string provided, throwing an exception if it is badly formed.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <returns>IPNetAddress object.</returns>
        public static IPNetAddress Parse(string addr)
        {
            if (TryParse(addr, out IPNetAddress o))
            {
                return o;
            }

            throw new ArgumentException("Unable to recognise object :" + addr);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public override bool Contains(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            var altAddress = NetworkAddressOf(address, PrefixLength);
            return NetworkAddress.Address.Equals(altAddress.Item1);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public override bool Contains(IPObject address)
        {
            if (address is IPHost addressObj && addressObj.HasAddress)
            {
                foreach (IPAddress addr in addressObj.GetAddresses())
                {
                    if (Contains(addr))
                    {
                        return true;
                    }
                }
            }
            else if (address is IPNetAddress netaddrObj)
            {
                var netAddress = NetworkAddress.NetworkAddress.Address;
                // Have the same network address, but different subnets?
                if (netAddress.Equals(netaddrObj.NetworkAddress.Address))
                {
                    return NetworkAddress.PrefixLength <= netaddrObj.PrefixLength;
                }

                var altAddress = NetworkAddressOf(netaddrObj.Address, PrefixLength);
                return netAddress.Equals(altAddress.Item1);
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPObject? other)
        {
            if (other is IPNetAddress otherObj && !Address.Equals(IPAddress.None) && !otherObj.Address.Equals(IPAddress.None))
            {
                return Address.AddressFamily == otherObj.Address.AddressFamily &&
                    Address.Equals(otherObj.Address) &&
                    PrefixLength == otherObj.PrefixLength;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPAddress address)
        {
            if (address != null && !address.Equals(IPAddress.None) && !Address.Equals(IPAddress.None))
            {
                if (Address.AddressFamily == address.AddressFamily)
                {
                    return address.Equals(Address);
                }

                if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        Address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
                        address.ToString(),
                        StringComparison.OrdinalIgnoreCase);
                }

                if (Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
                        Address.ToString(),
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Exists(IPAddress address)
        {
            if (address != null && !Address.Equals(IPAddress.None))
            {
                return Address.Equals(address);
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Returns a textual representation of this object.
        /// </summary>
        /// <param name="shortVersion">Set to true, if the subnet is to be included as part of the address.</param>
        /// <returns>String representation of this object.</returns>
        public string ToString(bool shortVersion)
        {
            if (!Address.Equals(IPAddress.None))
            {
                if (Address.Equals(IPAddress.Any))
                {
                    return "Any IP4 Address";
                }

                if (Address.Equals(IPAddress.IPv6Any))
                {
                    return "Any IP6 Address";
                }

                if (Address.Equals(IPAddress.Broadcast))
                {
                    return "All Addreses";
                }

                if (shortVersion)
                {
                    return Address.ToString();
                }

                return $"{Address}/{PrefixLength}";
            }

            return string.Empty;
        }
    }
}
