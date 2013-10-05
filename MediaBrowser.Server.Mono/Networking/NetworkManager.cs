using MediaBrowser.Common.Implementations.Networking;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MediaBrowser.ServerApplication.Networking
{
    /// <summary>
    /// Class NetUtils
    /// </summary>
    public class NetworkManager : BaseNetworkManager, INetworkManager
    {
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
        /// Uses the DllImport : NetServerEnum with all its required parameters
        /// (see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/netmgmt/netmgmt/netserverenum.asp
        /// for full details or method signature) to retrieve a list of domain SV_TYPE_WORKSTATION
        /// and SV_TYPE_SERVER PC's
        /// </summary>
        /// <returns>Arraylist that represents all the SV_TYPE_WORKSTATION and SV_TYPE_SERVER
        /// PC's in the Domain</returns>
        public IEnumerable<string> GetNetworkDevices()
        {
			return new List<string> ();
        }
    }
}
