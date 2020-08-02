#nullable enable

using System;
using System.Net;
using System.Net.Sockets;

namespace MediaBrowser.Common.Networking
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
        /// Gets or sets the user defined functions that need storage in this object.
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public abstract IPAddress Address { get; set; }

        /// <summary>
        /// Gets the object's IP address.
        /// </summary>
        public abstract IPAddress Mask { get; }

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
        /// Tests to see if the ip address is an AIPIPA address. (169.254.x.x).
        /// </summary>
        /// <param name="address">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsAIPIPA(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (!address.Equals(IPAddress.None))
            {
                if (address.IsIPv6LinkLocal)
                {
                    return true;
                }

                byte[] b = address.GetAddressBytes();
                return b[0] == 169 && b[1] == 254;
            }

            return false;
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
                byte[] b = address.GetAddressBytes();
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return CompareByteArray(b, Ipv4Loopback, 4);
                }

                return CompareByteArray(b, Ipv6Loopback, 16);
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
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] octet = address.GetAddressBytes();

                    return (octet[0] == 10) ||
                        (octet[0] == 172 && octet[1] >= 16 && octet[1] <= 31) || // RFC1918
                        (octet[0] == 192 && octet[1] == 168) || // RFC1918
                        (octet[0] == 127); // RFC1122
                }
                else
                {
                    if (address.IsIPv6SiteLocal)
                    {
                        return true;
                    }

                    byte[] octet = address.GetAddressBytes();
                    uint word = (uint)(octet[0] << 8);

                    return (word == 0xfc00 && word <= 0xfdff) // Unique local address. (fc00::/7)
                           || word == 0x100; // Discard prefix.
                }
            }

            return false;
        }

        /// <summary>
        /// Convert a subnet mask in CIDR notation to a dotted decimal string value. IPv4 only.
        /// </summary>
        /// <param name="cidr">Subnet mask in CIDR notation.</param>
        /// <returns>String value of the subnet mask in dotted decimal notation.</returns>
        public static IPAddress CidrToMask(byte cidr)
        {
            uint addr = 0xFFFFFFFF << (32 - cidr);
            addr =
                ((addr & 0xff000000) >> 24) |
                ((addr & 0x00ff0000) >> 8) |
                ((addr & 0x0000ff00) << 8) |
                ((addr & 0x000000ff) << 24);
            return new IPAddress(addr);
        }

        /// <summary>
        /// Convert a mask to a CIDR. IPv4 only.
        /// https://stackoverflow.com/questions/36954345/get-cidr-from-netmask.
        /// </summary>
        /// <param name="mask">Subnet mask.</param>
        /// <returns>Byte CIDR representing the mask.</returns>
        public static int MaskToCidr(IPAddress mask)
        {
            if (mask == null)
            {
                throw new ArgumentNullException(nameof(mask));
            }

            int cidrnet = 0;
            if (!mask.Equals(IPAddress.Any))
            {
                byte[] bytes = mask.GetAddressBytes();

                var zeroed = false;
                for (var i = 0; i < bytes.Length; i++)
                {
                    for (int v = bytes[i]; (v & 0xFF) != 0; v <<= 1)
                    {
                        if (zeroed)
                        {
                            // Invalid netmask.
                            return ~cidrnet;
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
        /// Returns the Network address of an ip address.
        /// </summary>
        /// <param name="address">IP address.</param>
        /// <param name="mask">Submask.</param>
        /// <returns>The network ip address of the subnet.</returns>
        public static IPAddress NetworkAddress(IPAddress address, IPAddress mask)
        {
            if (address == null || mask == null)
            {
                throw new ArgumentNullException(address == null ? nameof(address) : nameof(address));
            }

            if (address.Equals(IPAddress.None) || (mask.Equals(IPAddress.Any) && address.AddressFamily != AddressFamily.InterNetworkV6))
            {
                throw new ArgumentException("{0} must contain a value.", address.Equals(IPAddress.None) ? nameof(address) : nameof(mask));
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] addressBytes4 = address.GetAddressBytes();
                byte[] maskBytes4 = mask.GetAddressBytes();

                if (addressBytes4.Length != maskBytes4.Length)
                {
                    throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
                }

                byte[] networkAddress4 = new byte[addressBytes4.Length];
                for (int i = 0; i < networkAddress4.Length; i++)
                {
                    networkAddress4[i] = (byte)(addressBytes4[i] & maskBytes4[i]);
                }

                return new IPAddress(networkAddress4);
            }

            // First 64 bits contain the routing prefix.
            byte[] addressBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Array.Copy(address.GetAddressBytes(), addressBytes, 8);
            return new IPAddress(addressBytes);
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
        /// Removes IP6 addresses from this object.
        /// </summary>
        public virtual void RemoveIP6()
        {
            // This method only peforms a function in the IPHost implementation of IPObject.
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
            return Exists(ip);
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPObject other)
        {
            if (other != null && other is IPObject otherObj)
            {
                return !Address.Equals(IPAddress.None) && Address.Equals(otherObj.Address);
            }

            return false;
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public override bool Equals(object other)
        {
            if (other != null && other is IPObject otherObj)
            {
                return Equals(otherObj);
            }

            return false;
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPObject address)
        {
            return Equals(address);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="address">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPAddress address)
        {
            return Equals(address);
        }

        /// <summary>
        /// Returns true if IP exists in this parameter.
        /// </summary>
        /// <param name="address">Address to check for.</param>
        /// <returns>Existential result.</returns>
        public virtual bool Exists(IPAddress address)
        {
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Address.Equals(IPAddress.None) ? 0 : Address.GetHashCode();
        }

        /// <summary>
        /// Compares two byte arrays.
        /// </summary>
        /// <param name="src">Array one.</param>
        /// <param name="dest">Array two.</param>
        /// <param name="len">Length of both arrays. Must be the same.</param>
        /// <returns>True if the two arrays match.</returns>
        internal static bool CompareByteArray(byte[] src, byte[] dest, byte len)
        {
            for (int i = 0; i < len; i++)
            {
                if (src[i] != dest[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
