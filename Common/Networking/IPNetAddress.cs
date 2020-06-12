namespace Common.Networking
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// An object that holds and IP address and subnet mask.
    /// </summary>
    public class IPNetAddress : IPObject
    {
        /// <summary>
        /// Object's IP address.
        /// </summary>
        private IPAddress _address;

        /// <summary>
        /// Object's subnet mask.
        /// </summary>
        private IPAddress? _mask;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="ip">Address to assign.</param>
        public IPNetAddress(IPAddress ip)
        {
            _address = ip;
            _mask = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        /// <param name="subnet">Mask to assign.</param>
        public IPNetAddress(IPAddress address, IPAddress? subnet)
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
                if (Address.AddressFamily == AddressFamily.InterNetworkV6)
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
                if (value == null)
                {
                    throw new ArgumentException("Unable to assign null.");
                }

                _address = value;
            }
        }

        /// <summary>
        /// Gets the subnet mask of this object..
        /// </summary>
        public IPAddress? Mask => _mask;

        /// <summary>
        /// Try to parse the address and subnet strings into an IPNetAddress object.
        /// </summary>
        /// <param name="addr">IP address to parse. Can be CIDR or X.X.X.X notation.</param>
        /// <param name="ip">Resultant object.</param>
        /// <returns>True if the values parsed successfully. False if not, resulting in ip being null.</returns>
        public static bool TryParse(string addr, out IPNetAddress? ip)
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

                        if (IPAddress.TryParse(tokens[1], out IPAddress mask))
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
            if (IPNetAddress.TryParse(addr, out IPNetAddress? o))
            {
#pragma warning disable CS8603 // Possible null reference return.
                return o;
#pragma warning restore CS8603 // Possible null reference return.
            }

            throw new ArgumentException("Unable to recognise object :" + addr);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public override bool Contains(IPAddress ip)
        {
            IPAddress? nwAdd1 = IPObject.NetworkAddress(Address, _mask);
            IPAddress? nwAdd2 = IPObject.NetworkAddress(ip, _mask);

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
        public override bool Equals(IPObject other)
        {
            if (Address != null
                && other is IPNetAddress ipObj
                && ipObj.Address != null)
            {
                if (Address.AddressFamily == ipObj.Address.AddressFamily)
                {
                    // Compare only the address for IPv6, but both Address and Mask for IPv4.

                    if (Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (Mask != null)
                        {
                            // Return true if ipObj is a host and we're a network and the host matches ours.
                            bool eqAdd = Address.Equals(ipObj.Address);
                            return (eqAdd && Mask.Equals(ipObj.Mask)) ||
                                (eqAdd && ipObj.Mask != null && ipObj.Mask.Equals(IPAddress.Broadcast));
                        }

                        return Address.Equals(ipObj.Address);
                    }

                    if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return Address.Equals(ipObj.Address);
                    }
                }
                else if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        Address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
#pragma warning disable CA1062 // Validate arguments of public methods : "ip has a value here."
                        other.ToString(),
#pragma warning restore CA1062 // Validate arguments of public methods
                        StringComparison.OrdinalIgnoreCase);
                }
                else if (Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        other.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
                        Address.ToString(),
                        StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPAddress ip)
        {
            if (Address != null
                && ip != null)
            {
                if (Address.AddressFamily == ip.AddressFamily)
                {
                    return ip.Equals(Address);
                }

                if (Address.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        Address.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
                        ip.ToString(),
                        StringComparison.OrdinalIgnoreCase);
                }

                if (Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // Is one an ipv4 to ipv6 mapping?
                    return string.Equals(
                        ip.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase),
                        Address.ToString(),
                        StringComparison.OrdinalIgnoreCase);
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
        public override string ToString()
        {
            if (Address != null)
            {
                if (Mask != null)
                {
                    return $"{Address}/" + IPObject.MaskToCidr(Mask);
                }

                return Address.ToString();
            }

            return string.Empty;
        }
    }
}
