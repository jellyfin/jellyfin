#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace MediaBrowser.Common.Net
{
    public interface INetworkManager
    {
        event EventHandler NetworkChanged;

        /// <summary>
        /// Gets or sets a function to return the list of user defined LAN addresses.
        /// </summary>
        Func<string[]> LocalSubnetsFn { get; set; }

        /// <summary>
        /// Gets a random port TCP number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedTcpPort();

        /// <summary>
        /// Gets a random port UDP number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedUdpPort();

        /// <summary>
        /// Returns the MAC Address from first Network Card in Computer.
        /// </summary>
        /// <returns>The MAC Address.</returns>
        List<PhysicalAddress> GetMacAddresses();

        /// <summary>
        /// Determines whether [is in private address space] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in private address space] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInPrivateAddressSpace(string endpoint);

        /// <summary>
        /// Determines whether [is in private address space 10.x.x.x] [the specified endpoint] and exists in the subnets returned by GetSubnets().
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in private address space 10.x.x.x] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInPrivateAddressSpaceAndLocalSubnet(string endpoint);

        /// <summary>
        /// Determines whether [is in local network] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in local network] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInLocalNetwork(string endpoint);

        /// <summary>
        /// Investigates an caches a list of interface addresses, excluding local link and LAN excluded addresses.
        /// </summary>
        /// <returns>The list of ipaddresses.</returns>
        IPAddress[] GetLocalIpAddresses();

        /// <summary>
        /// Checks if the given address falls within the ranges given in [subnets]. The addresses in subnets can be hosts or subnets in the CIDR format.
        /// </summary>
        /// <param name="addressString">The address to check.</param>
        /// <param name="subnets">If true, check against addresses in the LAN settings surrounded by brackets ([]).</param>
        /// <returns><c>true</c>if the address is in at least one of the given subnets, <c>false</c> otherwise.</returns>
        bool IsAddressInSubnets(string addressString, string[] subnets);

        /// <summary>
        /// Returns true if address is in the LAN list in the config file.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="excludeInterfaces">If true, check against addresses in the LAN settings which have [] arroud and return true if it matches the address give in address.</param>
        /// <param name="excludeRFC">If true, returns false if address is in the 127.x.x.x or 169.128.x.x range.</param>
        /// <returns><c>false</c>if the address isn't in the LAN list, <c>true</c> if the address has been defined as a LAN address.</returns>
        bool IsAddressInSubnets(IPAddress address, bool excludeInterfaces, bool excludeRFC);

        /// <summary>
        /// Checks if address is in the LAN list in the config file.
        /// </summary>
        /// <param name="address1">Source address to check.</param>
        /// <param name="address2">Destination address to check against.</param>
        /// <param name="subnetMask">Destination subnet to check against.</param>
        /// <returns><c>true/false</c>depending on whether address1 is in the same subnet as IPAddress2 with subnetMask.</returns>
        bool IsInSameSubnet(IPAddress address1, IPAddress address2, IPAddress subnetMask);

        /// <summary>
        /// Returns the subnet mask of an interface with the given address.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <returns>Returns the subnet mask of an interface with the given address, or null if an interface match cannot be found.</returns>
        IPAddress GetLocalIpSubnetMask(IPAddress address);
    }
}
