using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaBrowser.Controller.IO
{
    public static class FileData
    {
        public const int MAX_PATH = 260;
        public const int MAX_ALTERNATE = 14;

        public static WIN32_FIND_DATA GetFileData(string fileName)
        {
            WIN32_FIND_DATA data;
            IntPtr handle = FindFirstFile(fileName, out data);
            if (handle == IntPtr.Zero)
                throw new IOException("FindFirstFile failed");
            FindClose(handle);
            return data;
        }

        [DllImport("kernel32")]
        private static extern IntPtr FindFirstFile(string fileName, out WIN32_FIND_DATA data);

        [DllImport("kernel32")]
        private static extern bool FindClose(IntPtr hFindFile);


    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_DATA
    {
        public FileAttributes dwFileAttributes;
        public FILETIME ftCreationTime;
        public FILETIME ftLastAccessTime;
        public FILETIME ftLastWriteTime;
        public int nFileSizeHigh;
        public int nFileSizeLow;
        public int dwReserved0;
        public int dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FileData.MAX_PATH)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = FileData.MAX_ALTERNATE)]
        public string cAlternate;

        public bool IsDirectory
        {
            get
            {
                return dwFileAttributes.HasFlag(FileAttributes.Directory);
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return ParseFileTime(ftCreationTime);
            }
        }

        public DateTime LastAccessTime
        {
            get
            {
                return ParseFileTime(ftLastAccessTime);
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return ParseFileTime(ftLastWriteTime);
            }
        }

        private DateTime ParseFileTime(FILETIME filetime)
        {
            long highBits = filetime.dwHighDateTime;
            highBits = highBits << 32;
            return DateTime.FromFileTime(highBits + (long)filetime.dwLowDateTime);
        }
    }

}
