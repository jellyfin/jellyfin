using System;
using System.IO;
using System.Runtime.InteropServices;

﻿using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Linq;

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

            data.Path = fileName;
            return data;
        }

        public static IEnumerable<WIN32_FIND_DATA> GetFileSystemEntries(string path, string searchPattern)
        {
            string lpFileName = Path.Combine(path, searchPattern);

            WIN32_FIND_DATA lpFindFileData;
            var handle = FindFirstFile(lpFileName, out lpFindFileData);

            if (handle == IntPtr.Zero)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr != 2 && hr != 0x12)
                {
                    throw new IOException("GetFileSystemEntries failed");
                }
                yield break;
            }

            if (IsValid(lpFindFileData.cFileName))
            {
                yield return lpFindFileData;
            }

            while (FindNextFile(handle, out lpFindFileData) != IntPtr.Zero)
            {
                if (IsValid(lpFindFileData.cFileName))
                {
                    lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);
                    yield return lpFindFileData;
                }
            }

            FindClose(handle);
        }

        private static bool IsValid(string cFileName)
        {
            if (cFileName.Equals(".", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (cFileName.Equals("..", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindFirstFile(string fileName, out WIN32_FIND_DATA data);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA data);

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

        public bool IsHidden
        {
            get
            {
                return dwFileAttributes.HasFlag(FileAttributes.Hidden);
            }
        }

        public bool IsSystemFile
        {
            get
            {
                return dwFileAttributes.HasFlag(FileAttributes.System);
            }
        }

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

        public string Path { get; set; }
    }

}
