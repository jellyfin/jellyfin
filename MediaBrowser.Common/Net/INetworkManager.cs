#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Networking;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Interface for the NetworkManager class.
    /// </summary>
    public interface INetworkManager
    {
        /// <summary>
        /// Event triggered on network changes.
        /// </summary>
        event EventHandler NetworkChanged;

        /// <summary>
        /// Gets a value indicating whether IP6 is enabled.
        /// </summary>
        bool IsIP6Enabled { get; }

        /// <summary>
        /// Gets the IP4Loopback address host.
        /// </summary>
        public IPNetAddress IP4Loopback { get; }

        /// <summary>
        /// Gets the IP6Loopback address host.
        /// </summary>
        public IPNetAddress IP6Loopback { get; }

        /// <summary>
        /// Gets returns the remote address filter.
        /// </summary>
        NetCollection RemoteAddressFilter { get; }

        /// <summary>
        /// Calculates the list of interfaces to use for Kestrel.
        /// </summary>
        /// <returns>A NetCollection object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items
        /// to represent any address.</returns>
        NetCollection GetAllBindInterfaces();

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// The priority of selection is as follows:-
        /// User interface preference (private/public), depending upon the source subnet(if known).
        /// If the user specified bind interfaces to use:-
        ///  The bind interface that contains the source subnet.
        ///  The first bind interface specified by the user
        /// If the source is from a public subnet address range and the user hasn't specified any bind addresses:-
        ///  The first public interface that isn't a loopback and contains the source subnet.
        ///  The first public interface that isn't a loopback. Priority is given to interfaces with gateways.
        ///  An internal interface if there are no public ip addresses.
        ///
        /// If the source is from a private subnet address range and the user hasn't specified any bind addresses:-
        ///  The first private interface that contains the source subnet.
        ///  The first private interface that isn't a loopback. Priority is given to interfaces with gateways.
        ///
        /// If no interfaces meet any of these criteria, the IPv4 loopback address is returned.
        ///
        /// Interface that have been specifically excluded from binding are not used in any of the calculations.
        /// IPv6 addresses follow the system wide setting.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        IPAddress GetBindInterface(object source);

        /// <summary>
        /// Checks to see if the ip address is specifically excluded in LocalNetworkAddresses.
        /// </summary>
        /// <param name="address">IP address to check.</param>
        /// <returns>True if it is.</returns>
        bool IsExcludedInterface(IPAddress address);

        /// <summary>
        /// Gets a random port number that is currently available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedUdpPort();

        /// <summary>
        /// Event triggered when configuration is changed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">New configuration.</param>
        void ConfigurationUpdated(object sender, EventArgs e);

        /// <summary>
        /// Get a list of all the MAC addresses associated with active interfaces.
        /// </summary>
        /// <returns>List of MAC addresses.</returns>
        List<PhysicalAddress> GetMacAddresses();

        /// <summary>
        /// Returns true if the address is a private address.
        /// The config option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="addr">Address to check.</param>
        /// <returns>True or False.</returns>
        public bool IsPrivateAddressRange(IPObject addr);

        /// <summary>
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(string endpoint);

        /// <summary>
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(IPNetAddress endpoint);

        /// <summary>
        /// Parses an array of strings into a NetCollection.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <param name="bracketed">When true, only include values in []. When false, ignore bracketed values.</param>
        /// <returns>IPCollection object containing the value strings.</returns>
        NetCollection CreateIPCollection(string[] values, bool bracketed = false);

        /// <summary>
        /// Returns all the internal Bind interface addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        NetCollection GetInternalBindAddresses();

        /// <summary>
        /// Checks to see if an IP address is still a valid interface address.
        /// </summary>
        /// <param name="address">IP address to check.</param>
        /// <returns>True if it is.</returns>
        bool IsValidInterfaceAddress(IPAddress address);

        /// <summary>
        /// Returns true if the IP address is in the excluded list.
        /// </summary>
        /// <param name="ip">IP to check.</param>
        /// <returns>True if excluded.</returns>
        bool IsExcluded(IPAddress ip);

        /// <summary>
        /// Gets the filtered LAN ip addresses.
        /// </summary>
        /// <param name="filter">Optional filter for the list.</param>
        /// <returns>Returns a filtered list of LAN addresses.</returns>
        NetCollection GetFilteredLANSubnets(NetCollection? filter = null);

        /// <summary>
        /// Returns true if the IP address in address2 is within the network address1/subnetMask.
        /// </summary>
        /// <param name="subnetIP">Subnet IP.</param>
        /// <param name="subnetMask">Subnet Mask.</param>
        /// <param name="address">Address to check.</param>
        /// <returns>True if address is in the subnet.</returns>
        bool IsInSameSubnet(IPAddress subnetIP, IPAddress subnetMask, IPAddress address);
    }
}
