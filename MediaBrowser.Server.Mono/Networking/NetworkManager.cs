using MediaBrowser.Common.Implementations.Networking;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
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
    }
}
