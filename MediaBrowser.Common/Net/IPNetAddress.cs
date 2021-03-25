#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// An object that holds and IP address and subnet mask.
    /// </summary>
    public class IPNetAddress
    {
        /// <summary>
        /// IPv4 multicast address.
        /// </summary>
        public static readonly IPAddress SSDPMulticastIPv4 = IPAddress.Parse("239.255.255.250");

        /// <summary>
        /// IPv6 local link multicast address.
        /// </summary>
        public static readonly IPAddress SSDPMulticastIPv6LinkLocal = IPAddress.Parse("ff02::C");

        /// <summary>
        /// IPv6 site local multicast address.
        /// </summary>
        public static readonly IPAddress SSDPMulticastIPv6SiteLocal = IPAddress.Parse("ff05::C");

        /// <summary>
        /// IP4Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP4Loopback = IPNetAddress.Parse("127.0.0.1/8");

        /// <summary>
        /// IP6Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP6Loopback = IPNetAddress.Parse("::1");

        /// <summary>
        /// IPv6 Loopback address.
        /// </summary>
        protected static readonly byte[] Ipv6Loopback = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        /// <summary>
        /// IPv4 Loopback address.
        /// </summary>
        protected static readonly byte[] Ipv4Loopback = { 127, 0, 0, 1 };

        /// <summary>
        /// Object's IP address.
        /// </summary>
        private IPAddress _address;

        /// <summary>
        /// The network address of this object.
        /// </summary>
        private IPNetAddress? _networkAddress;

        private bool _isNetwork;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        public IPNetAddress(IPAddress address)
        {
            _address = address;
            PrefixLength = (byte)(Address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">IP Address.</param>
        /// <param name="prefixLength">Mask as a CIDR.</param>
        public IPNetAddress(IPAddress address, byte prefixLength)
        {
            if (address?.IsIPv4MappedToIPv6 ?? throw new ArgumentNullException(nameof(address)))
            {
                _address = address.MapToIPv4();
            }
            else
            {
                _address = address;
            }

            PrefixLength = prefixLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        protected IPNetAddress()
        {
            _address = IPAddress.None;
        }

        /// <summary>
        /// Gets or sets a user defined value that is associated with this object.
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets the object's IP address.
        /// </summary>
        public virtual IPAddress Address
        {
            get
            {
                return _address;
            }
        }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public virtual byte PrefixLength { get; set; }

        /// <summary>
        /// Gets the AddressFamily of this object.
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                return Address == null ? AddressFamily.Unspecified : Address.AddressFamily;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the object is a network address.
        /// </summary>
        public bool IsNetwork
        {
            get => _isNetwork;

            set
            {
                if (!_isNetwork)
                {
                    var result = NetworkAddressOf(Address, PrefixLength);
                    _address = result.Address;
                    PrefixLength = result.PrefixLength;
                }

                _isNetwork = value;
            }
        }

        /// <summary>
        /// Gets a value indicating the network address of this object. Lazy implimentation.
        /// </summary>
        public IPNetAddress NetworkAddress
        {
            get
            {
                if (_isNetwork)
                {
                    return this;
                }

                if (_networkAddress == null)
                {
                    var result = NetworkAddressOf(Address, PrefixLength);
                    _networkAddress = new IPNetAddress(result.Address, result.PrefixLength)
                    {
                        Tag = this.Tag,
                        _isNetwork = true
                    };
                }

                return _networkAddress;
            }
        }

        /// <summary>
        /// Returns the network address of an object.
        /// </summary>
        /// <param name="address">IP Address to convert.</param>
        /// <param name="prefixLength">Subnet prefix.</param>
        /// <returns>IPAddress.</returns>
        public static (IPAddress Address, byte PrefixLength) NetworkAddressOf(IPAddress address, byte prefixLength)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (IsLoopback(address))
            {
                return (Address: address, PrefixLength: prefixLength);
            }

            // An ip address is just a list of bytes, each one representing a segment on the network.
            // This separates the IP address into octets and calculates how many octets will need to be altered or set to zero dependant upon the
            // prefix length value. eg. /16 on a 4 octet ip4 address (192.168.2.240) will result in the 2 and the 240 being zeroed out.
            // Where there is not an exact boundary (eg /23), mod is used to calculate how many bits of this value are to be kept.

            // GetAddressBytes
            Span<byte> addressBytes = stackalloc byte[address.AddressFamily == AddressFamily.InterNetwork ? 4 : 16];
            address.TryWriteBytes(addressBytes, out _);

            int div = prefixLength / 8;
            int mod = prefixLength % 8;
            if (mod != 0)
            {
                // Prefix length is counted right to left, so subtract 8 so we know how many bits to clear.
                mod = 8 - mod;

                // Shift out the bits from the octet that we don't want, by moving right then back left.
                addressBytes[div] = (byte)((int)addressBytes[div] >> mod << mod);
                // Move on the next byte.
                div++;
            }

            // Blank out the remaining octets from mod + 1 to the end of the byte array. (192.168.2.240/16 becomes 192.168.0.0)
            for (int octet = div; octet < addressBytes.Length; octet++)
            {
                addressBytes[octet] = 0;
            }

            // Return the network address for the prefix.
            return (Address: new IPAddress(addressBytes), PrefixLength: prefixLength);
        }

        /// <summary>
        /// Tests to see if the ip address is a Loopback address.
        /// </summary>
        /// <param name="address">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsLoopback(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            return address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback);
        }

        /// <summary>
        /// Tests to see if the ip address is an IP6 address.
        /// </summary>
        /// <param name="address">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsIP6(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                return false;
            }

            return address.AddressFamily == AddressFamily.InterNetworkV6;
        }

        /// <summary>
        /// Tests to see if the address in the private address range.
        /// </summary>
        /// <param name="address">Object to test.</param>
        /// <returns>True if it contains a private address.</returns>
        public static bool IsPrivateAddressRange(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // GetAddressBytes
                Span<byte> ip4octet = stackalloc byte[4];
                address.TryWriteBytes(ip4octet, out _);

                return (ip4octet[0] == 10)
                        || (ip4octet[0] == 172 && ip4octet[1] >= 16 && ip4octet[1] <= 31) // RFC1918
                        || (ip4octet[0] == 192 && ip4octet[1] == 168) // RFC1918
                        || (ip4octet[0] == 127); // RFC1122
            }

            // GetAddressBytes
            Span<byte> octet = stackalloc byte[16];
            address.TryWriteBytes(octet, out _);

            uint word = (uint)(octet[0] << 8) + octet[1];

            return (word >= 0xfe80 && word <= 0xfebf) // fe80::/10 :Local link.
                    || (word >= 0xfc00 && word <= 0xfdff); // fc00::/7 :Unique local address.
        }

        /// <summary>
        /// Returns true if the IPAddress contains an IP6 Local link address.
        /// </summary>
        /// <param name="address">IPAddress object to check.</param>
        /// <returns>True if it is a local link address.</returns>
        /// <remarks>
        /// See https://stackoverflow.com/questions/6459928/explain-the-instance-properties-of-system-net-ipaddress
        /// it appears that the IPAddress.IsIPv6LinkLocal is out of date.
        /// </remarks>
        public static bool IsIPv6LinkLocal(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily != AddressFamily.InterNetworkV6)
            {
                return false;
            }

            // GetAddressBytes
            Span<byte> octet = stackalloc byte[16];
            address.TryWriteBytes(octet, out _);
            uint word = (uint)(octet[0] << 8) + octet[1];

            return word >= 0xfe80 && word <= 0xfebf; // fe80::/10 :Local link.
        }

        /// <summary>
        /// Convert a subnet mask in CIDR notation to a dotted decimal string value. IPv4 only.
        /// </summary>
        /// <param name="cidr">Subnet mask in CIDR notation.</param>
        /// <param name="family">IPv4 or IPv6 family.</param>
        /// <returns>String value of the subnet mask in dotted decimal notation.</returns>
        public static IPAddress CidrToMask(byte cidr, AddressFamily family)
        {
            uint addr = 0xFFFFFFFF << (family == AddressFamily.InterNetwork ? 32 : 128 - cidr);
            addr = ((addr & 0xff000000) >> 24)
                   | ((addr & 0x00ff0000) >> 8)
                   | ((addr & 0x0000ff00) << 8)
                   | ((addr & 0x000000ff) << 24);
            return new IPAddress(addr);
        }

        /// <summary>
        /// Convert a mask to a CIDR. IPv4 only.
        /// https://stackoverflow.com/questions/36954345/get-cidr-from-netmask.
        /// </summary>
        /// <param name="mask">Subnet mask.</param>
        /// <returns>Byte CIDR representing the mask.</returns>
        public static byte MaskToCidr(IPAddress mask)
        {
            if (mask == null)
            {
                throw new ArgumentNullException(nameof(mask));
            }

            byte cidrnet = 0;
            if (!mask.Equals(IPAddress.Any))
            {
                // GetAddressBytes
                Span<byte> bytes = stackalloc byte[mask.AddressFamily == AddressFamily.InterNetwork ? 4 : 16];
                mask.TryWriteBytes(bytes, out _);

                var zeroed = false;
                for (var i = 0; i < bytes.Length; i++)
                {
                    for (int v = bytes[i]; (v & 0xFF) != 0; v <<= 1)
                    {
                        if (zeroed)
                        {
                            // Invalid netmask.
                            return (byte)~cidrnet;
                        }

                        if ((v & 0x80) == 0)
                        {
                            zeroed = true;
                        }
                        else
                        {
                            cidrnet++;
                        }
                    }
                }
            }

            return cidrnet;
        }

        /// <summary>
        /// Try to parse the address and subnet strings into an IPNetAddress object.
        /// </summary>
        /// <param name="addr">IP address to parse. Can be CIDR or X.X.X.X notation.</param>
        /// <param name="ip">Resultant object.</param>
        /// <param name="ipTypes"><see cref="IpClassType"/> to filter on.</param>
        /// <returns>True if the values parsed successfully. False if not, resulting in the IP being null.</returns>
        /// <remarks>For security purposes, this will treat ip addreses with a mask of 0 as invalid unless the address is 'Any'.</remarks>
        public static bool TryParse(string addr, [NotNullWhen(true)] out IPNetAddress? ip, IpClassType ipTypes)
        {
            if (string.IsNullOrEmpty(addr))
            {
                ip = null;
                return false;
            }

            addr = addr.Trim();

            // Is it a network?
            string[] tokens = addr.Split("/");

            if (tokens.Length > 2)
            {
                ip = null;
                return false;
            }

            if (IPAddress.TryParse(tokens[0].TrimEnd(), out var res))
            {
                if (((res.AddressFamily == AddressFamily.InterNetwork) && ipTypes == IpClassType.Ip6Only) ||
                    ((res.AddressFamily == AddressFamily.InterNetworkV6) && ipTypes == IpClassType.Ip4Only))
                {
                    ip = null;
                    return false;
                }

                if (tokens.Length == 1)
                {
                    ip = new IPNetAddress(res);
                    return true;
                }

                var subnet = tokens[1].TrimStart();

                // Is the subnet part a cidr?
                if (int.TryParse(subnet, out int cidr))
                {
                    // If the cidr out of bounds for the ip type, or is it zero and the ip address isn't 'Any', it's invalid.
                    if (cidr < 0 ||
                        ((cidr > 32) && (res.AddressFamily == AddressFamily.InterNetwork)) ||
                        ((cidr > 128) && (res.AddressFamily == AddressFamily.InterNetworkV6)) ||
                        (cidr == 0 && (!res.Equals(IPAddress.Any) && !res.Equals(IPAddress.IPv6Any))))
                    {
                        ip = null;
                        return false;
                    }

                    ip = new IPNetAddress(res, (byte)cidr);
                    return true;
                }

                // Is the subnet in x.y.a.b form?
                if (IPAddress.TryParse(subnet, out IPAddress? mask))
                {
                    if (mask.Equals(IPAddress.Any))
                    {
                        ip = null;
                        return false;
                    }

                    ip = new IPNetAddress(res, MaskToCidr(mask));
                    return true;
                }
            }

            ip = null;
            return false;
        }

        /// <summary>
        /// Parses the string provided, throwing an exception if it is badly formed.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <returns>IPNetAddress object.</returns>
        /// <remarks>For security purposes, this will treat ip addreses with a mask of 0 as invalid unless the address is 'Any'.</remarks>
        public static IPNetAddress Parse(string addr)
        {
            if (IPNetAddress.TryParse(addr, out IPNetAddress? o, IpClassType.IpBoth))
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
        public virtual bool Contains(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (Address == null || NetworkAddress.Address == null)
            {
                return false;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            var altAddress = NetworkAddressOf(address, PrefixLength);
            return NetworkAddress.Address.Equals(altAddress.Address) && NetworkAddress.PrefixLength >= altAddress.PrefixLength;
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPNetAddress address)
        {
            if (Address == null)
            {
                return false;
            }

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
                // Have the same network address, but different subnets?
                if (NetworkAddress.Address.Equals(netaddrObj.NetworkAddress.Address))
                {
                    return NetworkAddress.PrefixLength <= netaddrObj.PrefixLength;
                }

                var altAddress = NetworkAddressOf(netaddrObj.Address!, PrefixLength);
                return NetworkAddress.Address.Equals(altAddress.Address);
            }

            return false;
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPNetAddress other)
        {
            if (other is IPNetAddress otherObj)
            {
                return Address.Equals(otherObj.Address) && PrefixLength == otherObj.PrefixLength;
            }

            return false;
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public bool Equals(IPAddress ip)
        {
            if (ip == null)
            {
                return false;
            }

            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            return Address.Equals(ip);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Returns a textual representation of this object.
        /// </summary>
        /// <param name="shortVersion">Set to true, if the subnet is to be excluded as part of the address.</param>
        /// <returns>String representation of this object.</returns>
        public string ToString(bool shortVersion)
        {
            if (Address == null)
            {
                return "None";
            }

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
                return "Any Address";
            }

            if (shortVersion)
            {
                return Address.ToString();
            }

            return $"{Address}/{PrefixLength}";
        }

        /// <summary>
        /// Tests to see if this object is a Loopback address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsLoopback()
        {
            return IsLoopback(Address);
        }

        /// <summary>
        /// Tests to see if this object is an IPv6 address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsIP6()
        {
            return IsIP6(Address);
        }

        /// <summary>
        /// Returns true if this IP address is in the RFC private address range.
        /// </summary>
        /// <returns>True this object has a private address.</returns>
        public virtual bool IsPrivateAddressRange()
        {
            return IsPrivateAddressRange(Address);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }
}
