using MediaBrowser.Common.Implementations.Networking;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace MediaBrowser.ServerApplication.Networking
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
            Logger.Info("Getting network shares from {0}", path);
            return new ShareCollection(path).OfType<Share>().Select(ToNetworkShare);
        }

        /// <summary>
        /// To the network share.
        /// </summary>
        /// <param name="share">The share.</param>
        /// <returns>NetworkShare.</returns>
        private NetworkShare ToNetworkShare(Share share)
        {
            return new NetworkShare
            {
                Name = share.NetName,
                Path = share.Path,
                Remark = share.Remark,
                Server = share.Server,
                ShareType = ToNetworkShareType(share.ShareType)
            };
        }

        /// <summary>
        /// To the type of the network share.
        /// </summary>
        /// <param name="shareType">Type of the share.</param>
        /// <returns>NetworkShareType.</returns>
        /// <exception cref="System.ArgumentException">Unknown share type</exception>
        private NetworkShareType ToNetworkShareType(ShareType shareType)
        {
            if (shareType.HasFlag(ShareType.Special))
            {
                return NetworkShareType.Special;
            }
            if (shareType.HasFlag(ShareType.Device))
            {
                return NetworkShareType.Device;
            }
            if (shareType.HasFlag(ShareType.Disk))
            {
                return NetworkShareType.Disk;
            }
            if (shareType.HasFlag(ShareType.IPC))
            {
                return NetworkShareType.Ipc;
            }
            if (shareType.HasFlag(ShareType.Printer))
            {
                return NetworkShareType.Printer;
            }
            throw new ArgumentException("Unknown share type");
        }

        /// <summary>
        /// Uses the DllImport : NetServerEnum with all its required parameters
        /// (see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/netmgmt/netmgmt/netserverenum.asp
        /// for full details or method signature) to retrieve a list of domain SV_TYPE_WORKSTATION
        /// and SV_TYPE_SERVER PC's
        /// </summary>
        /// <returns>Arraylist that represents all the SV_TYPE_WORKSTATION and SV_TYPE_SERVER
        /// PC's in the Domain</returns>
        private List<string> GetNetworkDevicesInternal()
        {
            //local fields
            const int MAX_PREFERRED_LENGTH = -1;
            var SV_TYPE_WORKSTATION = 1;
            var SV_TYPE_SERVER = 2;
            IntPtr buffer = IntPtr.Zero;
            IntPtr tmpBuffer = IntPtr.Zero;
            var entriesRead = 0;
            var totalEntries = 0;
            var resHandle = 0;
            var sizeofINFO = Marshal.SizeOf(typeof(_SERVER_INFO_100));

            var returnList = new List<string>();

            try
            {
                //call the DllImport : NetServerEnum with all its required parameters
                //see http://msdn.microsoft.com/library/default.asp?url=/library/en-us/netmgmt/netmgmt/netserverenum.asp
                //for full details of method signature
                var ret = NativeMethods.NetServerEnum(null, 100, ref buffer, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, SV_TYPE_WORKSTATION | SV_TYPE_SERVER, null, out resHandle);

                //if the returned with a NERR_Success (C++ term), =0 for C#
                if (ret == 0)
                {
                    //loop through all SV_TYPE_WORKSTATION and SV_TYPE_SERVER PC's
                    for (var i = 0; i < totalEntries; i++)
                    {
                        //get pointer to, Pointer to the buffer that received the data from
                        //the call to NetServerEnum. Must ensure to use correct size of 
                        //STRUCTURE to ensure correct location in memory is pointed to
                        tmpBuffer = new IntPtr((Int64)buffer + (i * sizeofINFO));
                        //Have now got a pointer to the list of SV_TYPE_WORKSTATION and 
                        //SV_TYPE_SERVER PC's, which is unmanaged memory
                        //Needs to Marshal data from an unmanaged block of memory to a 
                        //managed object, again using STRUCTURE to ensure the correct data
                        //is marshalled 
                        var svrInfo = (_SERVER_INFO_100)Marshal.PtrToStructure(tmpBuffer, typeof(_SERVER_INFO_100));

                        //add the PC names to the ArrayList
                        if (!string.IsNullOrEmpty(svrInfo.sv100_name))
                        {
                            returnList.Add(svrInfo.sv100_name);
                        }
                    }
                }
            }
            finally
            {
                //The NetApiBufferFree function frees 
                //the memory that the NetApiBufferAllocate function allocates
                NativeMethods.NetApiBufferFree(buffer);
            }

            return returnList;
        }

        /// <summary>
        /// Gets available devices within the domain
        /// </summary>
        /// <returns>PC's in the Domain</returns>
        public IEnumerable<FileSystemEntryInfo> GetNetworkDevices()
        {
            return GetNetworkDevicesInternal().Select(c => new FileSystemEntryInfo
            {
                Name = c,
                Path = NetworkPrefix + c,
                Type = FileSystemEntryType.NetworkComputer
            });
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

        /// <summary>
        /// Gets the network prefix.
        /// </summary>
        /// <value>The network prefix.</value>
        private string NetworkPrefix
        {
            get
            {
                var separator = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
                return separator + separator;
            }
        }
    }

}
