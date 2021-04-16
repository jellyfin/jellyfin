#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
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
        /// Gets a value indicating whether iP6 is enabled.
        /// </summary>
        bool IsIP6Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether iP4 is enabled.
        /// </summary>
        bool IsIP4Enabled { get; }

        /// <summary>
        /// Calculates the list of interfaces to use for Kestrel.
        /// </summary>
        /// <returns>A Collection{IPNetAddress} object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items
        /// to represent any address.</returns>
        IEnumerable<IPNetAddress> GetAllBindInterfaces();

        /// <summary>
        /// Returns a collection containing the loopback interfaces.
        /// </summary>
        /// <returns>Collection{IPNetAddress}.</returns>
        IReadOnlyList<IPNetAddress> GetLoopbacks();

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
        string GetBindInterface(IPNetAddress source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPNetAddress, out int?)"/>.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(HttpRequest source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPNetAddress, out int?)"/>.
        /// </summary>
        /// <param name="source">IP address of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(IPAddress source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system url's. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindInterface(IPNetAddress, out int?)"/>.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP Address to use, or loopback address if all else fails.</returns>
        string GetBindInterface(string source, out int? port);

        /// <summary>
        /// Get a list of all the MAC addresses associated with active interfaces.
        /// </summary>
        /// <returns>List of MAC addresses.</returns>
        IReadOnlyCollection<PhysicalAddress> GetMacAddresses();

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
        bool IsInLocalNetwork(IPNetAddress address);

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
        bool TryParseInterface(string token, out Collection<IPNetAddress>? result);

        /// <summary>
        /// Parses an array of strings into a Collection{IPNetAddress}.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <param name="negated">When true, only include values beginning with !. When false, ignore ! values.</param>
        /// <param name="combineNetworks">When true, networks are merged where possible.</param>
        /// <returns>IPCollection object containing the value strings.</returns>
        Collection<IPNetAddress> CreateIPCollection(string[] values, bool negated, bool combineNetworks);

        /// <summary>
        /// Returns all the internal Bind interface addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        IPNetAddress[] GetInternalBindAddresses();

        /// <summary>
        /// Checks to see if <paramref name="remoteIp"/> has access.
        /// </summary>
        /// <param name="remoteIp">IP Address of client.</param>
        /// <returns><b>True</b> if has access, otherwise <b>false</b>.</returns>
        bool HasRemoteAccess(IPAddress remoteIp);
    }
}
