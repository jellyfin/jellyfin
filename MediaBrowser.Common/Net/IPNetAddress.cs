#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// An object that holds and IP address and subnet mask.
    /// </summary>
    public class IPNetAddress
    {
        private readonly byte _prefixLength;

        /// <summary>
        /// IP4Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP4Loopback = IPNetAddress.Parse("127.0.0.1/8");

        /// <summary>
        /// IP6Loopback address host.
        /// </summary>
        public static readonly IPNetAddress IP6Loopback = new (IPAddress.IPv6Loopback);

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
            _address = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

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
        /// Gets the object's IP address.
        /// </summary>
        public virtual byte PrefixLength
        {
            get => _prefixLength;
            protected init
            {
                if (value > (Address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), _address.ToString() + '/' + value.ToString(CultureInfo.CurrentCulture));
                }

                _prefixLength = value;
            }
        }

        /// <summary>
        /// Gets the AddressFamily of this object.
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                return Address.Equals(IPAddress.None) ? AddressFamily.Unspecified : Address.AddressFamily;
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
                    _address = NetworkAddressOf(Address, PrefixLength);
                    _isNetwork = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating the network address of this object. Lazy implementation.
        /// </summary>
        public IPNetAddress Network
        {
            get
            {
                if (IsNetwork)
                {
                    return this;
                }

                // If we have calculated this before, use that
                if (_networkAddress == null)
                {
                    var address = NetworkAddressOf(Address, PrefixLength);
                    _networkAddress = new IPNetAddress(address, PrefixLength)
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
        public static IPAddress NetworkAddressOf(IPAddress address, byte prefixLength)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (prefixLength > (address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128))
            {
                throw new ArgumentOutOfRangeException(nameof(prefixLength));
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
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
                addressBytes[div] = (byte)(addressBytes[div] >> mod << mod);
                // Move on the next byte.
                div++;
            }

            // Blank out the remaining octets from mod + 1 to the end of the byte array. (192.168.2.240/16 becomes 192.168.0.0)
            for (int octet = div; octet < addressBytes.Length; octet++)
            {
                addressBytes[octet] = 0;
            }

            // Return the network address for the prefix.
            return new IPAddress(addressBytes);
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
            string[] tokens = addr.Split('/');

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

            if (address.AddressFamily != AddressFamily)
            {
                return false;
            }

            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            var altAddress = NetworkAddressOf(address, PrefixLength);
            return Network.Address.Equals(altAddress) && Network.PrefixLength >= PrefixLength;
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPNetAddress ip)
        {
            if (ip is IPHost { HasAddress: true } addressObj)
            {
                foreach (IPAddress addr in addressObj.GetAddresses())
                {
                    if (addr.AddressFamily == AddressFamily && Contains(addr))
                    {
                        return true;
                    }
                }
            }
            else if (ip.AddressFamily == AddressFamily)
            {
                // Have the same network address, but different subnets?
                if (Network.Address.Equals(ip.Network.Address))
                {
                    return Network.PrefixLength <= ip.PrefixLength;
                }

                var altAddress = NetworkAddressOf(ip.Address, PrefixLength);
                return Network.Address.Equals(altAddress);
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
            return Address.Equals(other.Address) && PrefixLength == other.PrefixLength;
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public bool Equals(IPAddress ip)
        {
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
            if (Address.Equals(IPAddress.None))
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
        public bool IsLoopback()
        {
            var addr = Address;
            if (addr.IsIPv4MappedToIPv6)
            {
                addr = Address.MapToIPv4();
            }

            return addr.Equals(IPAddress.Loopback) || addr.Equals(IPAddress.IPv6Loopback);
        }

        /// <summary>
        /// Returns true if this IP address is in the RFC private address range.
        /// </summary>
        /// <returns>True this object has a private address.</returns>
        public bool IsPrivateAddressRange()
        {
            var address = Address;
            if (address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                // GetAddressBytes
                Span<byte> ip4Octet = stackalloc byte[4];
                address.TryWriteBytes(ip4Octet, out _);

                return (ip4Octet[0] == 10)
                       || (ip4Octet[0] == 172 && ip4Octet[1] >= 16 && ip4Octet[1] <= 31) // RFC1918
                       || (ip4Octet[0] == 192 && ip4Octet[1] == 168) // RFC1918
                       || (ip4Octet[0] == 127); // RFC1122
            }

            // GetAddressBytes
            Span<byte> octet = stackalloc byte[16];
            address.TryWriteBytes(octet, out _);

            uint word = (uint)(octet[0] << 8) + octet[1];

            return (word >= 0xfe80 && word <= 0xfebf) // fe80::/10 :Local link.
                   || (word >= 0xfc00 && word <= 0xfdff); // fc00::/7 :Unique local address.
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }
}
