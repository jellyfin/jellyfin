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
        public bool IsIP6Enabled { get; }

        /// <summary>
        /// Initialises the object. Can't be in constructor, as network changes could happen before this class has initialised.
        /// </summary>
        /// <param name="ip6Enabled">Function that returns the EnableIPV6 config option.</param>
        /// <param name="subnets">Function that returns the LocalNetworkSubnets config option.</param>
        /// <param name="bindInterfaces">Function that returns the LocalNetworkAddresses config option.</param>
        void Initialise(Func<bool> ip6Enabled, Func<string[]> subnets, Func<string[]> bindInterfaces);

        /// <summary>
        /// Returns all the valid interfaces in config LocalNetworkAddresses.
        /// </summary>
        /// <returns>A NetCollection object containing all the interfaces to bind.</returns>
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
        int GetRandomUnusedTcpPort();

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
        public void NamedConfigurationUpdated(object sender, EventArgs e);

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
        /// Returns all filtered interface addresses that respond to ping.
        /// </summary>
        /// <param name="allowLoopback">Allow loopback addresses in the list.</param>
        /// <param name="limit">Limit the number of items in the response.</param>
        /// <returns>Returns a filtered list of interface addresses.</returns>
        public NetCollection GetPingableInterfaceAddresses(bool allowLoopback, int limit);

        /// <summary>
        /// Parses an array of strings into a NetCollection.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <param name="bracketed">When true, only include values in []. When false, ignore bracketed values.</param>
        /// <returns>IPCollection object containing the value strings.</returns>
        public NetCollection CreateIPCollection(string[] values, bool bracketed = false);

        /// <summary>
        /// Interface callback function.
        /// </summary>
        /// <param name="callback">Delegate function to call on each match.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>true or false.</returns>
        public NetCollection CallbackOnFilteredBindAddresses(Func<IPAddress, CancellationToken, Task<bool>> callback, CancellationToken cancellationToken);

        /// <summary>
        /// Returns all the filtered LAN interfaces addresses.
        /// </summary>
        /// <returns>An internal list of interfaces addresses.</returns>
        public NetCollection GetInternalInterfaceAddresses();

        /// <summary>
        /// Checks to see if an IP address is still a valid interface address.
        /// </summary>
        /// <param name="address">IP address to check.</param>
        /// <returns>True if it is.</returns>
        public bool IsValidInterfaceAddress(IPAddress address);

            /// <summary>
            /// Returns true if the IP address is in the excluded list.
            /// </summary>
            /// <param name="ip">IP to check.</param>
            /// <returns>True if excluded.</returns>
        public bool IsExcluded(IPAddress ip);

        /// <summary>
        /// Gets the filtered LAN ip addresses.
        /// </summary>
        /// <param name="filter">Filter for the list.</param>
        /// <returns>Returns a filtered list of LAN addresses.</returns>
        public NetCollection GetFilteredLANAddresses(NetCollection filter);

        /// <summary>
        /// Returns all the filtered LAN addresses.
        /// </summary>
        /// <returns>A filtered list of LAN subnets/IPs.</returns>
        public NetCollection GetLANAddresses();

        /// <summary>
        /// Returns all filtered IPv4 LAN interface addresses, regardless of IPv6 status.
        /// </summary>
        /// <returns>Returns a filtered list of IPV4 interface addresses.</returns>
        public NetCollection GetInternalIPv4InterfaceAddresses();
    }
}
