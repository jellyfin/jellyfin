using MediaBrowser.Model.Net;
using System.Collections.Generic;

namespace MediaBrowser.Common.Net
{
    public interface INetworkManager
    {
        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
        string GetLocalIpAddress();

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedPort();

        /// <summary>
        /// Creates the netsh URL registration.
        /// </summary>
        void AuthorizeHttpListening(string url);

        /// <summary>
        /// Adds the windows firewall rule.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        void AddSystemFirewallRule(int port, NetworkProtocol protocol);

        /// <summary>
        /// Removes the windows firewall rule.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        void RemoveSystemFirewallRule(int port, NetworkProtocol protocol);

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        string GetMacAddress();

        /// <summary>
        /// Gets available devices within the domain
        /// </summary>
        /// <returns>PC's in the Domain</returns>
        IEnumerable<string> GetNetworkDevices();

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        IEnumerable<NetworkShare> GetNetworkShares(string path);
    }
    /// <summary>
    /// Enum NetworkProtocol
    /// </summary>
    public enum NetworkProtocol
    {
        /// <summary>
        /// The TCP
        /// </summary>
        Tcp,
        /// <summary>
        /// The UDP
        /// </summary>
        Udp
    }
}