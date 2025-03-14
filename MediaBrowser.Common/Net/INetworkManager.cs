using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using MediaBrowser.Model.Net;
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
        /// Gets a value indicating whether IPv4 is enabled.
        /// </summary>
        bool IsIPv4Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether IPv6 is enabled.
        /// </summary>
        bool IsIPv6Enabled { get; }

        /// <summary>
        /// Calculates the list of interfaces to use for Kestrel.
        /// </summary>
        /// <returns>A IReadOnlyList{IPData} object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items
        /// to represent any address.</returns>
        /// <param name="individualInterfaces">When false, return <see cref="IPAddress.Any"/> or <see cref="IPAddress.IPv6Any"/> for all interfaces.</param>
        IReadOnlyList<IPData> GetAllBindInterfaces(bool individualInterfaces = false);

        /// <summary>
        /// Returns a list containing the loopback interfaces.
        /// </summary>
        /// <returns>IReadOnlyList{IPData}.</returns>
        IReadOnlyList<IPData> GetLoopbacks();

        /// <summary>
        /// Retrieves the bind address to use in system URLs. (Server Discovery, PlayTo, LiveTV, SystemInfo)
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
        ///  The first public interface that isn't a loopback.
        ///  The first internal interface that isn't a loopback.
        ///
        /// If the source is from a private subnet address range and the user hasn't specified any bind addresses:-
        ///  The first private interface that contains the source subnet.
        ///  The first private interface that isn't a loopback.
        ///
        /// If no interfaces meet any of these criteria, then a loopback address is returned.
        ///
        /// Interfaces that have been specifically excluded from binding are not used in any of the calculations.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP address to use, or loopback address if all else fails.</returns>
        string GetBindAddress(HttpRequest source, out int? port);

        /// <summary>
        /// Retrieves the bind address to use in system URLs. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// </summary>
        /// <param name="source">IP address of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <param name="skipOverrides">Optional boolean denoting if published server overrides should be ignored. Defaults to false.</param>
        /// <returns>IP address to use, or loopback address if all else fails.</returns>
        string GetBindAddress(IPAddress? source, out int? port, bool skipOverrides = false);

        /// <summary>
        /// Retrieves the bind address to use in system URLs. (Server Discovery, PlayTo, LiveTV, SystemInfo)
        /// If no bind addresses are specified, an internal interface address is selected.
        /// (See <see cref="GetBindAddress(IPAddress, out int?, bool)"/>.
        /// </summary>
        /// <param name="source">Source of the request.</param>
        /// <param name="port">Optional port returned, if it's part of an override.</param>
        /// <returns>IP address to use, or loopback address if all else fails.</returns>
        string GetBindAddress(string source, out int? port);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// </summary>
        /// <param name="address">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(string address);

        /// <summary>
        /// Returns true if the address is part of the user defined LAN.
        /// </summary>
        /// <param name="address">IP to check.</param>
        /// <returns>True if endpoint is within the LAN range.</returns>
        bool IsInLocalNetwork(IPAddress address);

        /// <summary>
        /// Attempts to convert the interface name to an IP address.
        /// eg. "eth1", or "enp3s5".
        /// </summary>
        /// <param name="intf">Interface name.</param>
        /// <param name="result">Resulting object's IP addresses, if successful.</param>
        /// <returns>Success of the operation.</returns>
        bool TryParseInterface(string intf, [NotNullWhen(true)] out IReadOnlyList<IPData>? result);

        /// <summary>
        /// Returns all internal (LAN) bind interface addresses.
        /// </summary>
        /// <returns>An list of internal (LAN) interfaces addresses.</returns>
        IReadOnlyList<IPData> GetInternalBindAddresses();

        /// <summary>
        /// Checks if <paramref name="remoteIP"/> has access to the server.
        /// </summary>
        /// <param name="remoteIP">IP address of the client.</param>
        /// <returns><b>True</b> if it has access, otherwise <b>false</b>.</returns>
        bool HasRemoteAccess(IPAddress remoteIP);
    }
}
