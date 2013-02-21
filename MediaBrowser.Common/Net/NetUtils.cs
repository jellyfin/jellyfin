using MediaBrowser.Common.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Class NetUtils
    /// </summary>
    public static class NetUtils
    {
        /// <summary>
        /// Gets the machine's local ip address
        /// </summary>
        /// <returns>IPAddress.</returns>
        public static IPAddress GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            return host.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
        }

        /// <summary>
        /// Gets a random port number that is currently available
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int GetRandomUnusedPort()
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
        /// <param name="urlPrefix">The URL prefix.</param>
        public static void CreateNetshUrlRegistration(string urlPrefix)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = string.Format("http add urlacl url={0} user=\"NT AUTHORITY\\Authenticated Users\"", urlPrefix),
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
        public static void AddWindowsFirewallRule(int port, NetworkProtocol protocol)
        {
            // First try to remove it so we don't end up creating duplicates
            RemoveWindowsFirewallRule(port, protocol);

            var args = string.Format("advfirewall firewall add rule name=\"Port {0}\" dir=in action=allow protocol={1} localport={0}", port, protocol);

            RunNetsh(args);
        }

        /// <summary>
        /// Removes the windows firewall rule.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="protocol">The protocol.</param>
        public static void RemoveWindowsFirewallRule(int port, NetworkProtocol protocol)
        {
            var args = string.Format("advfirewall firewall delete rule name=\"Port {0}\" protocol={1} localport={0}", port, protocol);

            RunNetsh(args);
        }

        /// <summary>
        /// Runs the netsh.
        /// </summary>
        /// <param name="args">The args.</param>
        private static void RunNetsh(string args)
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
        public static string GetMacAddress()
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
        public static IEnumerable<string> GetNetworkComputers()
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
