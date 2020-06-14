namespace Common.Networking
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// Gets a value indicating whether IP6 is enabled..
        /// </summary>
        bool IsIP6Enabled { get; }

        /// <summary>
        /// Returns all the valid interfaces in config LocalNetworkAddresses.
        /// </summary>
        /// <returns>A NetCollection object containing all the interfaces to bind.
        /// If all the interfaces are specified, and none are excluded, it returns zero items.</returns>
        NetCollection GetBindInterfaces();

        /// <summary>
        /// Returns all the excluded interfaces in config LocalNetworkAddresses.
        /// </summary>
        /// <returns>A NetCollection object containing all the excluded interfaces.</returns>
        NetCollection GetBindExclusions();

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
        /// Interface callback function that returns the IP address of the first callback that succeeds.
        /// </summary>
        /// <param name="callback">Delegate function to call for each ip.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>NetCollection object.</returns>
        NetCollection OnFilteredBindAddressesCallback(Func<IPObject, CancellationToken, Task<bool>> callback, CancellationToken cancellationToken);

        /// <summary>
        /// Returns all the filtered LAN interfaces addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        NetCollection GetInternalInterfaceAddresses();

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
        NetCollection GetFilteredLANAddresses(NetCollection? filter = null);
    }
}
