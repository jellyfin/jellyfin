using System.Globalization;
using System.Management;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MediaBrowser.Common.Implementations.NetworkManagement
{
    /// <summary>
    /// Class NetUtils
    /// </summary>
    public class NetworkManager : INetworkManager
    {
        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
        public string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            var ip = host.AddressList.LastOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);

            if (ip == null)
            {
                return null;
            }

            return ip.ToString();
        }

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// Creates the netsh URL registration.
        /// </summary>
        public void AuthorizeHttpListening(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = string.Format("http add urlacl url={0} user=\"NT AUTHORITY\\Authenticated Users\"", url),
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Adds the windows firewall rule.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        public void AddSystemFirewallRule(int port, NetworkProtocol protocol)
        {
            // First try to remove it so we don't end up creating duplicates
            RemoveSystemFirewallRule(port, protocol);

            var args = string.Format("advfirewall firewall add rule name=\"Port {0}\" dir=in action=allow protocol={1} localport={0}", port, protocol);

            RunNetsh(args);
        }

        /// <summary>
        /// Removes the windows firewall rule.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        public void RemoveSystemFirewallRule(int port, NetworkProtocol protocol)
        {
            var args = string.Format("advfirewall firewall delete rule name=\"Port {0}\" protocol={1} localport={0}", port, protocol);

            RunNetsh(args);
        }

        /// <summary>
        /// Runs the netsh.
        /// </summary>
        /// <param name="args">The args.</param>
        private void RunNetsh(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = args,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public string GetMacAddress()
        {
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            var macAddress = String.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (macAddress == String.Empty)  // only return MAC Address from first card
                {
                    try
                    {
                        if ((bool)mo["IPEnabled"]) macAddress = mo["MacAddress"].ToString();
                    }
                    catch
                    {
                        mo.Dispose();
                        return "";
                    }
                }
                mo.Dispose();
            }

            return macAddress.Replace(":", "");
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
            //local fields
            const int MAX_PREFERRED_LENGTH = -1;
            var SV_TYPE_WORKSTATION = 1;
            var SV_TYPE_SERVER = 2;
            var buffer = IntPtr.Zero;
            var tmpBuffer = IntPtr.Zero;
            var entriesRead = 0;
            var totalEntries = 0;
            var resHandle = 0;
            var sizeofINFO = Marshal.SizeOf(typeof(_SERVER_INFO_100));

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
                        tmpBuffer = new IntPtr((int)buffer + (i * sizeofINFO));
                        //Have now got a pointer to the list of SV_TYPE_WORKSTATION and 
                        //SV_TYPE_SERVER PC's, which is unmanaged memory
                        //Needs to Marshal data from an unmanaged block of memory to a 
                        //managed object, again using STRUCTURE to ensure the correct data
                        //is marshalled 
                        var svrInfo = (_SERVER_INFO_100)Marshal.PtrToStructure(tmpBuffer, typeof(_SERVER_INFO_100));

                        //add the PC names to the ArrayList
                        if (!string.IsNullOrEmpty(svrInfo.sv100_name))
                        {
                            yield return svrInfo.sv100_name;
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
        }


        /// <summary>
        /// Gets the network shares.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{NetworkShare}.</returns>
        public IEnumerable<NetworkShare> GetNetworkShares(string path)
        {
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
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <returns>IPEndPoint.</returns>
        public IPEndPoint Parse(string endpointstring)
        {
            return Parse(endpointstring, -1);
        }

        /// <summary>
        /// Parses the specified endpointstring.
        /// </summary>
        /// <param name="endpointstring">The endpointstring.</param>
        /// <param name="defaultport">The defaultport.</param>
        /// <returns>IPEndPoint.</returns>
        /// <exception cref="System.ArgumentException">Endpoint descriptor may not be empty.</exception>
        /// <exception cref="System.FormatException"></exception>
        private static IPEndPoint Parse(string endpointstring, int defaultport)
        {
            if (string.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                || defaultport > IPEndPoint.MaxPort))
            {
                throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultport));
            }

            string[] values = endpointstring.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                port = values.Length == 1 ? defaultport : GetPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = GetIPfromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = GetPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpointstring);
                    port = defaultport;
                }
            }
            else
            {
                throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", endpointstring));
            }

            if (port == -1)
                throw new ArgumentException(string.Format("No port specified: '{0}'", endpointstring));

            return new IPEndPoint(ipaddy, port);
        }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.FormatException"></exception>
        private static int GetPort(string p)
        {
            int port;

            if (!int.TryParse(p, out port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(string.Format("Invalid end point port '{0}'", p));
            }

            return port;
        }

        /// <summary>
        /// Gets the I pfrom host.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>IPAddress.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        private static IPAddress GetIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(string.Format("Host not found: {0}", p));

            return hosts[0];
        }
    }

}
