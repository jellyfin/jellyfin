using System.Net;
using System.Net.Sockets;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace MediaBrowser.Model.Net;

/// <summary>
/// Base network object class.
/// </summary>
public class IPData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IPData"/> class.
    /// </summary>
    /// <param name="address">The <see cref="IPAddress"/>.</param>
    /// <param name="subnet">The <see cref="IPNetwork"/>.</param>
    /// <param name="name">The interface name.</param>
    public IPData(IPAddress address, IPNetwork? subnet, string name)
    {
        Address = address;
        Subnet = subnet ?? (address.AddressFamily == AddressFamily.InterNetwork ? new IPNetwork(address, 32) : new IPNetwork(address, 128));
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IPData"/> class.
    /// </summary>
    /// <param name="address">The <see cref="IPAddress"/>.</param>
    /// <param name="subnet">The <see cref="IPNetwork"/>.</param>
    public IPData(IPAddress address, IPNetwork? subnet)
        : this(address, subnet, string.Empty)
    {
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
    /// Gets or sets a value indicating whether the network supports multicast.
    /// </summary>
    public bool SupportsMulticast { get; set; } = false;

    /// <summary>
    /// Gets or sets the interface name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the AddressFamily of the object.
    /// </summary>
    public AddressFamily AddressFamily
    {
        get
        {
            if (Address.Equals(IPAddress.None))
            {
                return Subnet.Prefix.AddressFamily.Equals(IPAddress.None)
                    ? AddressFamily.Unspecified
                    : Subnet.Prefix.AddressFamily;
            }
            else
            {
                return Address.AddressFamily;
            }
        }
    }
}
