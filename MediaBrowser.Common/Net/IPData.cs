using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Base network object class.
    /// </summary>
    public class IPData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IPData"/> class.
        /// </summary>
        /// <param name="address">An <see cref="IPAddress"/>.</param>
        /// <param name="subnet">The <see cref="IPNetwork"/>.</param>
        public IPData(
            IPAddress address,
            IPNetwork? subnet)
        {
            Address = address;
            Subnet = subnet ?? (address.AddressFamily == AddressFamily.InterNetwork ? new IPNetwork(address, 32) : new IPNetwork(address, 128));
            Name = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IPData"/> class.
        /// </summary>
        /// <param name="address">An <see cref="IPAddress"/>.</param>
        /// <param name="subnet">The <see cref="IPNetwork"/>.</param>
        /// <param name="name">The object's name.</param>
        public IPData(
            IPAddress address,
            IPNetwork? subnet,
            string name)
        {
            Address = address;
            Subnet = subnet ?? (address.AddressFamily == AddressFamily.InterNetwork ? new IPNetwork(address, 32) : new IPNetwork(address, 128));
            Name = name;
        }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public IPAddress Address { get; set; }

        /// <summary>
        /// Gets or sets the object's IP address.
        /// </summary>
        public IPNetwork Subnet { get; set; }

        /// <summary>
        /// Gets or sets the interface index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the interface name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the AddressFamily of this object.
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                return Address.Equals(IPAddress.None)
                    ? (Subnet.Prefix.AddressFamily.Equals(IPAddress.None)
                        ? AddressFamily.Unspecified : Subnet.Prefix.AddressFamily)
                    : Address.AddressFamily;
            }
        }
    }
}
