using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    public interface INetworkManager
    {
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
        /// Determines whether [is in private address space] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in private address space] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInPrivateAddressSpace(string endpoint);

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        IEnumerable<NetworkShare> GetNetworkShares(string path);

        /// <summary>
        /// Gets available devices within the domain
        /// </summary>
        /// <returns>PC's in the Domain</returns>
        IEnumerable<FileSystemEntryInfo> GetNetworkDevices();

        /// <summary>
        /// Determines whether [is in local network] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in local network] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInLocalNetwork(string endpoint);

        List<IpAddressInfo> GetLocalIpAddresses();

        IpAddressInfo ParseIpAddress(string ipAddress);

        bool TryParseIpAddress(string ipAddress, out IpAddressInfo ipAddressInfo);

        Task<IpAddressInfo[]> GetHostAddressesAsync(string host);
    }
}