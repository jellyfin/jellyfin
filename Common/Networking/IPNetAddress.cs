using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common.Networking
{
    /// <summary>
    /// An object that holds and IP address and subnet mask.
    /// </summary>
    public class IPNetAddress : IPObject
    {
        /// <summary>
        /// Object's subnet mask.
        /// </summary>
        private IPAddress _mask;

        /// <summary>
        /// Object's ip address.
        /// </summary>
        private IPAddress _address;

        /// <summary>
        /// Gets the IP Address of this object.
        /// </summary>
        public IPAddress Address => _address;

        /// <summary>
        /// Gets the subnet mask of this object.
        /// </summary>
        public IPAddress Mask => _mask;

        /// <summary>
        /// Convert a subnet mask in CIDR notation to a dotted decimal string value.
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
                else if (address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // First 64 bits contain the routing prefix.
                    byte[] addressBytes = new byte[8];
                    Array.Copy(address.GetAddressBytes(), addressBytes, 8);
                    return new IPAddress(addressBytes);
                }
            }

            return null;
        }

        /// <summary>
        /// Try to parse the address and subnet strings into an IPNetAddress object.
        /// </summary>
        /// <param name="addr">IP address to parse. Can be CIDR or X.X.X.X notation.</param>
        /// <param name="ip">Resultant object.</param>
        /// <returns>True if the values parsed successfully. False if not, resulting in ip being null.</returns>
        public static bool TryParse(string addr, out IPNetAddress ip)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                addr = addr.Trim();

                // Try to parse it as is.
                if (IPAddress.TryParse(addr, out IPAddress res))
                {
                    ip = new IPNetAddress(res, 32);
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
                            ip = new IPNetAddress(res, CidrToMask(cidr));
                            return true;
                        }

                        if (IPAddress.TryParse(tokens[0], out IPAddress mask))
                        {
                            ip = new IPNetAddress(res, mask);
                            return true;
                        }
                    }
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
        public static IPNetAddress Parse(string addr)
        {
            if (IPNetAddress.TryParse(addr, out IPNetAddress o))
            {
                return o;
            }

            throw new ArgumentException("Unable to recognise object :" + addr);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="ip">Address to assign.</param>
#pragma warning disable SA1201 // Elements should appear in the correct order
        public IPNetAddress(IPAddress ip)
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            _address = ip;
            _mask = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        /// <param name="subnet">Mask to assign.</param>
        public IPNetAddress(IPAddress address, IPAddress subnet)
        {
            _address = address;
            _mask = subnet;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">IP Address.</param>
        /// <param name="cidr">Mask as a CIDR.</param>
        public IPNetAddress(IPAddress address, byte cidr)
        {
            if (address != null)
            {
                _address = address;
                if (_address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _mask = null;
                }
                else
                {
                    _mask = CidrToMask(cidr);
                }
            }
            else
            {
                throw new ArgumentException("Address cannot be null.");
            }
        }

        /// <summary>
        /// Returns the Network address of this object.
        /// </summary>
        /// <returns>The Network IP address of our this object.</returns>
        public IPAddress NetworkAddress()
        {
            return IPNetAddress.NetworkAddress(_address, _mask);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public override bool Contains(IPAddress ip)
        {
            IPAddress nwAdd1 = IPNetAddress.NetworkAddress(_address, _mask);
            IPAddress nwAdd2 = IPNetAddress.NetworkAddress(ip, _mask);

            if (nwAdd1 != null && nwAdd2 != null)
            {
                return nwAdd1.Equals(nwAdd2);
            }

            return false;
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public override bool Contains(IPObject ip)
        {
            if (ip is IPHost ipObj)
            {
                if (ipObj.Addresses != null)
                {
                    foreach (IPAddress a in ipObj.Addresses)
                    {
                        if (Contains(a))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (ip is IPNetAddress naObj)
            {
                return Contains(naObj.Address);
            }

            return false;
        }

        /// <inheritdoc/>
        public override void Copy(IPObject ip)
        {
            if (ip is IPNetAddress obj)
            {
                _address = obj.Address;
                _mask = obj.Mask;
            }
            else
            {
                throw new InvalidCastException("Parameter is not an IPNetAddress.");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(IPObject ip)
        {
            if (Address != null)
            {
                if (ip is IPNetAddress ipObj)
                {
                    if (ipObj.Address != null)
                    {
                        if (Address.AddressFamily == ipObj.Address.AddressFamily)
                        {
                            // Compare only the address for IPv6, but both Address and Mask for IPv4.
                            if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                return Address.Equals(ipObj.Address);
                            }
                            else if (Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                if (Mask != null)
                                {
                                    return Address.Equals(ipObj.Address) && Mask.Equals(ipObj.Mask);
                                }

                                return Address.Equals(ipObj.Address);
                            }
                        }
                        else if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            // Is one an ipv4 to ipv6 mapping?
                            return string.Equals(Address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase), ip.ToString(), StringComparison.OrdinalIgnoreCase);
                        }
                        else if (Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            // Is one an ipv4 to ipv6 mapping?
                            return string.Equals(ip.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase), Address.ToString(), StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPAddress ip)
        {
            if (Address != null)
            {
                if (ip != null)
                {
                    if (Address.AddressFamily == ip.AddressFamily)
                    {
                        return ip.Equals(Address);
                    }
                    else if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Is one an ipv4 to ipv6 mapping?
                        return string.Equals(Address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase), ip.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // Is one an ipv4 to ipv6 mapping?
                        return string.Equals(ip.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase), Address.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Exists(IPAddress ip)
        {
            if (Address != null && ip != null)
            {
                return ip.Equals(Address);
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Exists(string addr)
        {
            if (Address != null && !string.IsNullOrEmpty(addr))
            {
                return Equals(IPNetAddress.Parse(addr));
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Exists(IPObject ip)
        {
            if (ip is IPNetAddress ipObj)
            {
                return Exists(ipObj.Address);
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Address != null)
            {
                if (Mask != null)
                {
                    return $"{Address}/{Mask}"; // {Address.ToString() + "/" + Mask.ToString();
                }

                return Address.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        protected override IPAddress GetAddressInternal()
        {
            return Address;
        }
    }
}
