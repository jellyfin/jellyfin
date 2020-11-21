#nullable enable
using System;
using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Base network object class.
    /// </summary>
    public abstract class IPObject : IEquatable<IPObject>
    {
        /// <summary>
        /// IPv6 Loopback address.
        /// </summary>
        protected static readonly byte[] Ipv6Loopback = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        /// <summary>
        /// IPv4 Loopback address.
        /// </summary>
        protected static readonly byte[] Ipv4Loopback = { 127, 0, 0, 1 };

        /// <summary>
        /// The network address of this object.
        /// </summary>
        private IPObject? _networkAddress;

        /// <summary>
        /// Gets or sets a user defined value that is associated with this object.
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public abstract IPAddress Address { get; set; }

        /// <summary>
        /// Gets the object's network address.
        /// </summary>
        public IPObject NetworkAddress => _networkAddress ??= CalculateNetworkAddress();

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public abstract byte PrefixLength { get; set; }

        /// <summary>
        /// Gets the AddressFamily of this object.
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                // Keep terms separate as Address performs other functions in inherited objects.
                IPAddress address = Address;
                return address.Equals(IPAddress.None) ? AddressFamily.Unspecified : address.AddressFamily;
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

            if (!address.Equals(IPAddress.None))
            {
                if (address.IsIPv4MappedToIPv6)
                {
                    address = address.MapToIPv4();
                }

                return address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback);
            }

            return false;
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
                address = address.MapToIPv4();
            }

            return !address.Equals(IPAddress.None) && (address.AddressFamily == AddressFamily.InterNetworkV6);
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

            if (!address.Equals(IPAddress.None))
            {
                if (address.IsIPv4MappedToIPv6)
                {
                    address = address.MapToIPv4();
                }

                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // GetAddressBytes
                    Span<byte> octet = stackalloc byte[4];
                    address.TryWriteBytes(octet, out _);

                    return (octet[0] == 10)
                           || (octet[0] == 172 && octet[1] >= 16 && octet[1] <= 31) // RFC1918
                           || (octet[0] == 192 && octet[1] == 168) // RFC1918
                           || (octet[0] == 127); // RFC1122
                }
                else
                {
                    // GetAddressBytes
                    Span<byte> octet = stackalloc byte[16];
                    address.TryWriteBytes(octet, out _);

                    uint word = (uint)(octet[0] << 8) + octet[1];

                    return (word >= 0xfe80 && word <= 0xfebf) // fe80::/10 :Local link.
                           || (word >= 0xfc00 && word <= 0xfdff); // fc00::/7 :Unique local address.
                }
            }

            return false;
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
        /// Tests to see if this object is a Loopback address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsLoopback()
        {
            return IsLoopback(Address);
        }

        /// <summary>
        /// Removes all addresses of a specific type from this object.
        /// </summary>
        /// <param name="family">Type of address to remove.</param>
        public virtual void Remove(AddressFamily family)
        {
            // This method only performs a function in the IPHost implementation of IPObject.
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

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPAddress ip)
        {
            if (ip != null)
            {
                if (ip.IsIPv4MappedToIPv6)
                {
                    ip = ip.MapToIPv4();
                }

                return !Address.Equals(IPAddress.None) && Address.Equals(ip);
            }

            return false;
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPObject? other)
        {
            if (other != null)
            {
                return !Address.Equals(IPAddress.None) && Address.Equals(other.Address);
            }

            return false;
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public abstract bool Contains(IPObject address);

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public abstract bool Contains(IPAddress address);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as IPObject);
        }

        /// <summary>
        /// Calculates the network address of this object.
        /// </summary>
        /// <returns>Returns the network address of this object.</returns>
        protected abstract IPObject CalculateNetworkAddress();
    }
}
