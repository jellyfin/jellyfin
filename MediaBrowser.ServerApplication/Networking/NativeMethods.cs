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

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FileTimeToSystemTime(
            [In] ref long fileTime,
            out SystemTime systemTime);

        [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptAcquireContextW(
            out IntPtr providerContext,
            [MarshalAs(UnmanagedType.LPWStr)] string container,
            [MarshalAs(UnmanagedType.LPWStr)] string provider,
            int providerType,
            int flags);

        [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptReleaseContext(
            IntPtr providerContext,
            int flags);

        [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptGenKey(
            IntPtr providerContext,
            int algorithmId,
            int flags,
            out IntPtr cryptKeyHandle);

        [DllImport("AdvApi32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDestroyKey(
            IntPtr cryptKeyHandle);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertStrToNameW(
            int certificateEncodingType,
            IntPtr x500,
            int strType,
            IntPtr reserved,
            [MarshalAs(UnmanagedType.LPArray)] [Out] byte[] encoded,
            ref int encodedLength,
            out IntPtr errorString);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CertCreateSelfSignCertificate(
            IntPtr providerHandle,
            [In] ref CryptoApiBlob subjectIssuerBlob,
            int flags,
            [In] ref CryptKeyProviderInformation keyProviderInformation,
            [In] ref CryptAlgorithmIdentifier algorithmIdentifier,
            [In] ref SystemTime startTime,
            [In] ref SystemTime endTime,
            IntPtr extensions);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertFreeCertificateContext(
            IntPtr certificateContext);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr CertOpenStore(
            [MarshalAs(UnmanagedType.LPStr)] string storeProvider,
            int messageAndCertificateEncodingType,
            IntPtr cryptProvHandle,
            int flags,
            IntPtr parameters);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertCloseStore(
            IntPtr certificateStoreHandle,
            int flags);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertAddCertificateContextToStore(
            IntPtr certificateStoreHandle,
            IntPtr certificateContext,
            int addDisposition,
            out IntPtr storeContextPtr);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CertSetCertificateContextProperty(
            IntPtr certificateContext,
            int propertyId,
            int flags,
            [In] ref CryptKeyProviderInformation data);

        [DllImport("Crypt32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PFXExportCertStoreEx(
            IntPtr certificateStoreHandle,
            ref CryptoApiBlob pfxBlob,
            IntPtr password,
            IntPtr reserved,
            int flags);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct SystemTime
    {
        public short Year;
        public short Month;
        public short DayOfWeek;
        public short Day;
        public short Hour;
        public short Minute;
        public short Second;
        public short Milliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CryptObjIdBlob
    {
        public uint cbData;
        public IntPtr pbData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CryptAlgorithmIdentifier
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public String pszObjId;
        public CryptObjIdBlob Parameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CryptoApiBlob
    {
        public int DataLength;
        public IntPtr Data;

        public CryptoApiBlob(int dataLength, IntPtr data)
        {
            this.DataLength = dataLength;
            this.Data = data;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CryptKeyProviderInformation
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ContainerName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ProviderName;
        public int ProviderType;
        public int Flags;
        public int ProviderParameterCount;
        public IntPtr ProviderParameters; // PCRYPT_KEY_PROV_PARAM
        public int KeySpec;
    }
}
