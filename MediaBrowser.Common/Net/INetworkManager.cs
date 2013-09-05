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
        IEnumerable<string> GetLocalIpAddresses();

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        /// <returns>System.Int32.</returns>
        int GetRandomUnusedPort();

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        string GetMacAddress();

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