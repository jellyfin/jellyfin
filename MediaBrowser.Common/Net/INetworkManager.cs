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

        Func<string[]> LocalSubnetsFn { get; set; }

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedTcpPort();

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
        /// Determines whether [is in local network] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in local network] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInLocalNetwork(string endpoint);

        IPAddress[] GetLocalIpAddresses(bool ignoreVirtualInterface);

        bool IsAddressInSubnets(string addressString, string[] subnets);

        bool IsInSameSubnet(IPAddress address1, IPAddress address2, IPAddress subnetMask);

        IPAddress GetLocalIpSubnetMask(IPAddress address);
    }
}
