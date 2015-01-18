using MediaBrowser.Common.Implementations.Networking;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System.Collections.Generic;

namespace MediaBrowser.Server.Mono.Networking
{
    /// <summary>
    /// Class NetUtils
    /// </summary>
    public class NetworkManager : BaseNetworkManager, INetworkManager
    {
        public NetworkManager(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        public IEnumerable<NetworkShare> GetNetworkShares(string path)
        {
			return new List<NetworkShare> ();
        }

        /// <summary>
        /// Gets available devices within the domain
        /// </summary>
        /// <returns>PC's in the Domain</returns>
		public IEnumerable<FileSystemEntryInfo> GetNetworkDevices()
        {
			return new List<FileSystemEntryInfo> ();
        }

        /// <summary>
        /// Generates a self signed certificate at the locatation specified by <paramref name="certificatePath"/>.
        /// </summary>
        /// <param name="certificatePath">The path to generate the certificate.</param>
        /// <param name="hostname">The common name for the certificate.</param>
        public void GenerateSelfSignedSslCertificate(string certificatePath, string hostname)
        {
            CertificateGenerator.CreateSelfSignCertificatePfx(certificatePath, hostname, Logger);
        }
    }
}
