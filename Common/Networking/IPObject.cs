namespace Common.Networking
{
    using System;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Base network object class.
    /// </summary>
    public abstract class IPObject : IEquatable<IPObject>
    {
        /// <summary>
        /// Defines the _ip6loopback.
        /// </summary>
        private static readonly byte[] _ip6loopback = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        /// <summary>
        /// Defines the _ip4loopback.
        /// </summary>
        private static readonly byte[] _ip4loopback = { 127, 0, 0, 1 };

        /// <summary>
        /// Gets or sets the user defined functions that need storage in this object..
        /// </summary>
        public int Tag { get; set; }

        /// <summary>
        /// Gets the AddressFamily of this object..
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                IPAddress addr = GetAddressInternal();
                if (addr != null)
                {
                    return addr.AddressFamily;
                }

                return AddressFamily.Unspecified;
            }
        }

        /// <summary>
        /// Tests to see if the ip address is an AIPIPA address. (169.254.x.x).
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsAIPIPA(IPAddress i)
        {
            if (i != null)
            {
                if (i.IsIPv6LinkLocal)
                {
                    return true;
                }

                byte[] b = i.GetAddressBytes();
                return b[0] == 169 && b[1] == 254;
            }

            return false;
        }

        /// <summary>
        /// Tests to see if the ip address is a Loopback address.
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsLoopback(IPAddress i)
        {
            if (i != null)
            {
                byte[] b = i.GetAddressBytes();
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    return CompareByteArray(b, _ip4loopback, 4);
                }

                return CompareByteArray(b, _ip6loopback, 16);
            }

            return false;
        }

        /// <summary>
        /// Tests to see if the ip address is an ip 6 address.
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsIP6(IPAddress i)
        {
            return (i != null) && (i.AddressFamily == AddressFamily.InterNetworkV6);
        }

        /// <summary>
        /// Tests to see if the address in i is in the private address ranges.
        /// </summary>
        /// <param name="i">Object to test.</param>
        /// <returns>True if it contains a private address.</returns>
        public static bool IsPrivateAddressRange(IPAddress i)
        {
            if (i != null)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] octet = i.GetAddressBytes();

                    return (octet[0] == 10) ||
                        (octet[0] == 172 && octet[1] >= 16 && octet[1] <= 31) || // RFC1918
                        (octet[0] == 192 && octet[1] == 168) || // RFC1918
                        (octet[0] == 127) || // RFC1122
                        (octet[0] == 169 && octet[1] == 254); // RFC3927
                }
                else
                {
                    if (i.IsIPv6SiteLocal)
                    {
                        return true;
                    }

                    byte[] octet = i.GetAddressBytes();

                    uint word = (uint)(octet[0] << 8) + octet[1];

                    return (word == 0xfc00 && word <= 0xfdff) // Unique local address.
                        || (word >= 0xfe80 && word <= 0xfebf) // Local link address.
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
            int cidrnet = 0;
            if (mask != null)
            {
                byte[] bytes = mask.GetAddressBytes();

                var zeroed = false;
                for (var i = 0; i < bytes.Length; i++)
                {
                    for (int v = bytes[i]; (v & 0xFF) != 0; v <<= 1)
                    {
                        if (zeroed)
                        {
                            // invalid netmask
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
            if (address != null)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (mask == null)
                    {
                        throw new ArgumentException("Mask required to calculate the network address.");
                    }

                    byte[] addressBytes = address.GetAddressBytes();
                    byte[] maskBytes = mask.GetAddressBytes();

                    if (addressBytes.Length != maskBytes.Length)
                    {
                        throw new ArgumentException("Lengths of IP address and subnet mask do not match.");
                    }

                    byte[] networkAddress = new byte[addressBytes.Length];
                    for (int i = 0; i < networkAddress.Length; i++)
                    {
                        networkAddress[i] = (byte)(addressBytes[i] & maskBytes[i]);
                    }

                    return new IPAddress(networkAddress);
                }

                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // First 64 bits contain the routing prefix.
                    byte[] addressBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    Array.Copy(address.GetAddressBytes(), addressBytes, 8);
                    return new IPAddress(addressBytes);
                }
            }

            return null;
        }

        /// <summary>
        /// Tests to see if this object is a Loopback address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsLoopback()
        {
            return IsLoopback(GetAddressInternal());
        }

        /// <summary>
        /// Removes IP6 addresses from this object.
        /// </summary>
        public virtual void RemoveIP6()
        {
        }

        /// <summary>
        /// Tests to see if this object is an ip 6 address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsIP6()
        {
            return IsIP6(GetAddressInternal());
        }

        /// <summary>
        /// Returns true if this IP address is in the RFC private address range.
        /// </summary>
        /// <returns>True this object has a private address.</returns>
        public virtual bool IsPrivateAddressRange()
        {
            return IsPrivateAddressRange(GetAddressInternal());
        }

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="other">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPObject other)
        {
            return GetAddressInternal().Equals(other.GetAddressInternal());
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
        /// <param name="obj">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as IPObject);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPObject ip)
        {
            return Equals(ip);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPAddress ip)
        {
            return this.Equals(ip);
        }

        /// <summary>
        /// Returns true if IP exists in this parameter.
        /// </summary>
        /// <param name="ip">Address to check for.</param>
        /// <returns>Existential result.</returns>
        public virtual bool Exists(IPAddress ip)
        {
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return GetAddressInternal().GetHashCode();
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

        /// <summary>
        /// Task that pings an IP address.
        /// </summary>
        /// <param name="ip">Host name to ping.</param>
        /// <returns>The result of the ping.</returns>
        protected static async Task<PingReply> PingAsyncInternal(IPAddress ip)
        {
            if (ip != null)
            {
#pragma warning disable IDE0063 // By putting Ping in a using, it ensures that it is disposed off immediately after use.
                using (Ping sender = new Ping())
#pragma warning restore IDE0063
                {
                    PingOptions options = new PingOptions
                    {
                        DontFragment = true
                    };

                    string data = "JellyFin Ping Request.!!!!!!!!!!";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);

                    return await sender.SendPingAsync(ip, 120, buffer, options).ConfigureAwait(false);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the address item of the ancestor objects to use in low level functons.
        /// </summary>
        /// <returns>IP address.</returns>
        protected abstract IPAddress GetAddressInternal();
    }
}
