using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using System.Collections.Generic;
using System.Net;

namespace MediaBrowser.Common.Net
{
    public interface INetworkManager
    {
        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
		IEnumerable<IPAddress> GetLocalIpAddresses();

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
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <returns>IPEndPoint.</returns>
        IPEndPoint Parse(string endpointstring);

        /// <summary>
        /// Determines whether [is in local network] [the specified endpoint].
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns><c>true</c> if [is in local network] [the specified endpoint]; otherwise, <c>false</c>.</returns>
        bool IsInLocalNetwork(string endpoint);

        /// <summary>
        /// Generates a self signed certificate at the locatation specified by <paramref name="certificatePath"/>.
        /// </summary>
        /// <param name="certificatePath">The path to generate the certificate.</param>
        /// <param name="hostname">The common name for the certificate.</param>
        void GenerateSelfSignedSslCertificate(string certificatePath, string hostname);
    }
}