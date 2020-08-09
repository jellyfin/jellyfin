#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MediaBrowser.Common.Networking;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;

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
        /// Gets a value indicating whether the system supports multi-socket binding.
        /// </summary>
        bool EnableMultiSocketBinding { get; }

        /// <summary>
        /// Gets the IP4Loopback address host.
        /// </summary>
        IPNetAddress IP4Loopback { get; }

        /// <summary>
        /// Gets the IP6Loopback address host.
        /// </summary>
        IPNetAddress IP6Loopback { get; }

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
        string GetBindInterface(object source);

        /// <summary>
        /// Called when the configuration has changed.
        /// </summary>
        /// <param name="sender">Object who made the call.</param>
        /// <param name="newConfig">New settings that are being applied.</param>
        void ConfigurationUpdating(object sender, GenericEventArgs<ServerConfiguration> newConfig);

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
        /// Returns a udp port based upon Configuration.UDPPort.
        /// </summary>
        /// <param name="portStr">Port Range, or empty/zero for a random port.</param>
        /// <returns>System.Int32.</returns>
        int GetPort(string portStr);

        /// <summary>
        /// Returns an unused UDP port number in the range specified.
        /// </summary>
        /// <param name="range">Upper and Lower boundary of ports to select.</param>
        /// <returns>System.Int32.</returns>
        int GetUdpPortFromRange((int min, int max) range);

        /// <summary>
        /// Get a list of all the MAC addresses associated with active interfaces.
        /// </summary>
        /// <returns>List of MAC addresses.</returns>
        List<PhysicalAddress> GetMacAddresses();

        /// <summary>
        /// Returns true if the address is a private address.
        /// The config option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <returns>True or False.</returns>
        bool IsPrivateAddressRange(IPObject address);

        /// <summary>
        /// Returns true if the two addresses are on the same machine.
        /// </summary>
        /// <param name="addr1">Address 1.</param>
        /// <param name="addr2">Address 2.</param>
        /// <returns>True if both are on this machine.</returns>
        public bool OnSameMachine(IPAddress addr1, IPAddress addr2);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// The config option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <returns>True or False.</returns>
        bool IsLANAddressRange(IPObject address);

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
        /// Calculates if the endpoint given falls within the LAN networks specified in config.
        /// </summary>
        /// <param name="endpoint">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(IPAddress endpoint);

        /// <summary>
        /// Attempts to convert the token to an IP address, permitting for interface descriptions and indexes.
        /// eg. "eth1", or "TP-LINK Wireless USB Adapter".
        /// </summary>
        /// <param name="token">Token to parse.</param>
        /// <param name="result">Resultant object if successful.</param>
        /// <returns>Success of the operation.</returns>
        bool TryParseInterface(string token, out IPNetAddress result);

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
        /// Returns true if the IP address is in the excluded list.
        /// </summary>
        /// <param name="ip">IP to check.</param>
        /// <returns>True if excluded.</returns>
        bool IsExcluded(EndPoint ip);

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

        /// <summary>
        /// Creates an UDP Socket.
        /// </summary>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        public Socket CreateUdpBroadcastSocket(int port);

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the SSDP multicast local admin group and binds it to the specified local port.
        /// </summary>
        /// <param name="address">IP Address to bind.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>A Socket.</returns>
        public Socket CreateSsdpUdpSocket(IPAddress address, int port);

        /// <summary>
        /// Creates a new UDP acceptSocket that is a member of the specified multicast IP address, and binds it to the specified local port.
        /// </summary>
        /// <param name="multicastTimeToLive">The multicast time to live value for the acceptSocket.</param>
        /// <param name="port">UDP port to bind.</param>
        /// <returns>Socket interface object.</returns>
        public Socket CreateUdpMulticastSocket(int multicastTimeToLive, int port);
    }
}
