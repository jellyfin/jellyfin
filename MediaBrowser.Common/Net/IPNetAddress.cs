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
    public class IPNetAddress : IPObject
    {
        /// <summary>
        /// Represents an IPNetAddress that has no value.
        /// </summary>
        public static readonly IPNetAddress None = new IPNetAddress(null);

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
        /// Object's IP address.
        /// </summary>
        private IPAddress? _address;

        /// <summary>
        /// Initializes a new instance of the <see cref="IPNetAddress"/> class.
        /// </summary>
        /// <param name="address">Address to assign.</param>
        public IPNetAddress(IPAddress? address)
        {
            _address = address;
            if (address != null)
            {
                PrefixLength = (byte)(address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
            }
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
        /// Gets or sets the object's IP address.
        /// </summary>
        public override IPAddress? Address
        {
            get
            {
                return _address;
            }

            set
            {
                _address = value;
            }
        }

        /// <inheritdoc/>
        public override byte PrefixLength { get; set; }

        /// <summary>
        /// Try to parse the address and subnet strings into an IPNetAddress object.
        /// </summary>
        /// <param name="addr">IP address to parse. Can be CIDR or X.X.X.X notation.</param>
        /// <param name="ip">Resultant object.</param>
        /// <returns>True if the values parsed successfully. False if not, resulting in the IP being null.</returns>
        /// <remarks>For security purposes, this will treat ip addreses with a mask of 0 as invalid unless the address is 'Any'.</remarks>
        public static bool TryParse(string addr, out IPNetAddress ip)
        {
            if (string.IsNullOrEmpty(addr))
            {
                ip = None;
                return false;
            }

            addr = addr.Trim();

            // Is it a network?
            string[] tokens = addr.Split("/");

            if (tokens.Length > 2)
            {
                ip = None;
                return false;
            }

            if (IPAddress.TryParse(tokens[0].TrimEnd(), out var res))
            {
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
                        ip = None;
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
                        ip = None;
                        return false;
                    }

                    ip = new IPNetAddress(res, MaskToCidr(mask));
                    return true;
                }
            }

            ip = None;
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
            if (TryParse(addr, out IPNetAddress o))
            {
                return o;
            }

            throw new ArgumentException("Unable to recognise object :" + addr);
        }

        /// <inheritdoc/>
        public override bool Contains(IPAddress address)
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

        /// <inheritdoc/>
        public override bool Contains(IPObject address)
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
                if (NetworkAddress.Address!.Equals(netaddrObj.NetworkAddress.Address))
                {
                    return NetworkAddress.PrefixLength <= netaddrObj.PrefixLength;
                }

                var altAddress = NetworkAddressOf(netaddrObj.Address!, PrefixLength);
                return NetworkAddress.Address.Equals(altAddress.Address);
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPObject? other)
        {
            if (other is IPNetAddress otherObj && Address != null && otherObj.Address != null)
            {
                return Address.Equals(otherObj.Address) &&
                    PrefixLength == otherObj.PrefixLength;
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool Equals(IPAddress ip)
        {
            if (ip != null && Address != null)
            {
                return ip.Equals(Address);
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

        /// <inheritdoc/>
        protected override IPObject CalculateNetworkAddress()
        {
            if (_address == null)
            {
                return IPNetAddress.None;
            }

            var value = NetworkAddressOf(_address, PrefixLength);
            return new IPNetAddress(value.Address, value.PrefixLength);
        }
    }
}
