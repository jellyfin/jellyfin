#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Http;

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
        /// Gets the published server urls list.
        /// </summary>
        Dictionary<IPNetAddress, string> PublishedServerUrls { get; }

        /// <summary>
        /// Gets a value indicating whether is all IPv6 interfaces are trusted as internal.
        /// </summary>
        bool TrustAllIP6Interfaces { get; }

        /// <summary>
        /// Gets the remote address filter.
        /// </summary>
        Collection<IPObject> RemoteAddressFilter { get; }

        /// <summary>
        /// Gets or sets a value indicating whether iP6 is enabled.
        /// </summary>
        bool IsIP6Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether iP4 is enabled.
        /// </summary>
        bool IsIP4Enabled { get; set; }

        /// <summary>
        /// Calculates the list of interfaces to use for Kestrel.
        /// </summary>
        /// <returns>A Collection{IPObject} object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items
        /// to represent any address.</returns>
        /// <param name="individualInterfaces">When false, return <see cref="IPAddress.Any"/> or <see cref="IPAddress.IPv6Any"/> for all interfaces.</param>
        Collection<IPObject> GetAllBindInterfaces(bool individualInterfaces = false);

        /// <summary>
        /// Returns a collection containing the loopback interfaces.
        /// </summary>
        /// <returns>Collection{IPObject}.</returns>
        Collection<IPObject> GetLoopbacks();

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// The priority of selection is as follows:-
        ///
        /// The value contained in the startup parameter --published-server-url.
        ///
        /// If the user specified custom subnet overrides, the correct subnet for the source address.
        ///
        /// If the user specified bind interfaces to use:-
        ///  The bind interface that contains the source subnet.
        ///  The first bind interface specified that suits best first the source's endpoint. eg. external or internal.
        ///
        /// If the source is from a public subnet address range and the user hasn't specified any bind addresses:-
        ///  The first public interface that isn't a loopback and contains the source subnet.
        ///  The first public interface that isn't a loopback. Priority is given to interfaces with gateways.
        ///  An internal interface if there are no public ip addresses.
        ///
        /// If the source is from a private subnet address range and the user hasn't specified any bind addresses:-
        ///  The first private interface that contains the source subnet.
        ///  The first private interface that isn't a loopback. Priority is given to interfaces with gateways.
        ///
        /// If no interfaces meet any of these criteria, then a loopback address is returned.
        ///
        /// Interface that have been specifically excluded from binding are not used in any of the calculations.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(IPObject source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPObject, out int?)"/>.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(HttpRequest source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPObject, out int?)"/>.
        /// </summary>
        /// <param name="source">IP address of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(IPAddress source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPObject, out int?)"/>.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(string source, out int? port);

        /// <summary>
        /// Checks to see if the ip address is specifically excluded in LocalNetworkAddresses.
        /// </summary>
        /// <param name="address">IP address to check.</param>
        /// <returns>True if it is.</returns>
        bool IsExcludedInterface(IPAddress address);

        /// <summary>
        /// Get a list of all the MAC addresses associated with active interfaces.
        /// </summary>
        /// <returns>List of MAC addresses.</returns>
        IReadOnlyCollection<PhysicalAddress> GetMacAddresses();

        /// <summary>
        /// Checks to see if the IP Address provided matches an interface that has a gateway.
        /// </summary>
        /// <param name="addressObj">IP to check. Can be an IPAddress or an IPObject.</param>
        /// <returns>Result of the check.</returns>
        bool IsGatewayInterface(IPObject? addressObj);

        /// <summary>
        /// Checks to see if the IP Address provided matches an interface that has a gateway.
        /// </summary>
        /// <param name="addressObj">IP to check. Can be an IPAddress or an IPObject.</param>
        /// <returns>Result of the check.</returns>
        bool IsGatewayInterface(IPAddress? addressObj);

        /// <summary>
        /// Returns true if the address is a private address.
        /// The configuration option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <returns>True or False.</returns>
        bool IsPrivateAddressRange(IPObject address);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// The configuration option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(string address);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// The configuration option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(IPObject address);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// The configuration option TrustIP6Interfaces overrides this functions behaviour.
        /// </summary>
        /// <param name="address">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(IPAddress address);

        /// <summary>
        /// Attempts to convert the token to an IP address, permitting for interface descriptions and indexes.
        /// eg. "eth1", or "TP-LINK Wireless USB Adapter".
        /// </summary>
        /// <param name="token">Token to parse.</param>
        /// <param name="result">Resultant object's ip addresses, if successful.</param>
        /// <returns>Success of the operation.</returns>
        bool TryParseInterface(string token, out Collection<IPObject>? result);

        /// <summary>
        /// Parses an array of strings into a Collection{IPObject}.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <param name="negated">When true, only include values beginning with !. When false, ignore ! values.</param>
        /// <returns>IPCollection object containing the value strings.</returns>
        Collection<IPObject> CreateIPCollection(string[] values, bool negated = false);

        /// <summary>
        /// Returns all the internal Bind interface addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        Collection<IPObject> GetInternalBindAddresses();

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
        Collection<IPObject> GetFilteredLANSubnets(Collection<IPObject>? filter = null);

        /// <summary>
        /// Checks to see if <paramref name="remoteIp"/> has access.
        /// </summary>
        /// <param name="remoteIp">IP Address of client.</param>
        /// <returns><b>True</b> if has access, otherwise <b>false</b>.</returns>
        bool HasRemoteAccess(IPAddress remoteIp);
    }
}
