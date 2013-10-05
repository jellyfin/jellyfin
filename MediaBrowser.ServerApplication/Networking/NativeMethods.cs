using System;
using System.Runtime.InteropServices;
using System.Security;

namespace MediaBrowser.ServerApplication.Networking
{
    /// <summary>
    /// Class NativeMethods
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class NativeMethods
    {
        //declare the Netapi32 : NetServerEnum method import
        /// <summary>
        /// Nets the server enum.
        /// </summary>
        /// <param name="ServerName">Name of the server.</param>
        /// <param name="dwLevel">The dw level.</param>
        /// <param name="pBuf">The p buf.</param>
        /// <param name="dwPrefMaxLen">The dw pref max len.</param>
        /// <param name="dwEntriesRead">The dw entries read.</param>
        /// <param name="dwTotalEntries">The dw total entries.</param>
        /// <param name="dwServerType">Type of the dw server.</param>
        /// <param name="domain">The domain.</param>
        /// <param name="dwResumeHandle">The dw resume handle.</param>
        /// <returns>System.Int32.</returns>
        [DllImport("Netapi32", CharSet = CharSet.Auto, SetLastError = true),
        SuppressUnmanagedCodeSecurity]

        public static extern int NetServerEnum(
            string ServerName, // must be null
            int dwLevel,
            ref IntPtr pBuf,
            int dwPrefMaxLen,
            out int dwEntriesRead,
            out int dwTotalEntries,
            int dwServerType,
            string domain, // null for login domain
            out int dwResumeHandle
            );

        //declare the Netapi32 : NetApiBufferFree method import
        /// <summary>
        /// Nets the API buffer free.
        /// </summary>
        /// <param name="pBuf">The p buf.</param>
        /// <returns>System.Int32.</returns>
        [DllImport("Netapi32", SetLastError = true),
        SuppressUnmanagedCodeSecurity]

        public static extern int NetApiBufferFree(
            IntPtr pBuf);
    }

    //create a _SERVER_INFO_100 STRUCTURE
    /// <summary>
    /// Struct _SERVER_INFO_100
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct _SERVER_INFO_100
    {
        /// <summary>
        /// The sv100_platform_id
        /// </summary>
        internal int sv100_platform_id;
        /// <summary>
        /// The sv100_name
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string sv100_name;
    }
}
