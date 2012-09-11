using MediaBrowser.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// Provides low level File access that is much faster than the File/Directory api's
    /// </summary>
    public static class FileData
    {
        public const int MAX_PATH = 260;
        public const int MAX_ALTERNATE = 14;
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        /// <summary>
        /// Gets information about a path
        /// </summary>
        public static WIN32_FIND_DATA GetFileData(string path)
        {
            WIN32_FIND_DATA data;
            IntPtr handle = FindFirstFile(path, out data);
            bool getFilename = false;

            if (handle == INVALID_HANDLE_VALUE && !Path.HasExtension(path))
            {
                if (!path.EndsWith("*"))
                {
                    Logger.LogInfo("Handle came back invalid for {0}. Since this is a directory we'll try appending \\*.", path);

                    FindClose(handle);

                    handle = FindFirstFile(Path.Combine(path, "*"), out data);

                    getFilename = true;
                }
            }

            if (handle == IntPtr.Zero)
            {
                throw new IOException("FindFirstFile failed");
            }

            if (getFilename)
            {
                data.cFileName = Path.GetFileName(path);
            }
            
            FindClose(handle);

            data.Path = path;
            return data;
        }

        /// <summary>
        /// Gets all file system entries within a foler
        /// </summary>
        public static IEnumerable<WIN32_FIND_DATA> GetFileSystemEntries(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, true, true);
        }

        /// <summary>
        /// Gets all files within a folder
        /// </summary>
        public static IEnumerable<WIN32_FIND_DATA> GetFiles(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, true, false);
        }

        /// <summary>
        /// Gets all sub-directories within a folder
        /// </summary>
        public static IEnumerable<WIN32_FIND_DATA> GetDirectories(string path, string searchPattern)
        {
            return GetFileSystemEntries(path, searchPattern, false, true);
        }

        /// <summary>
        /// Gets all file system entries within a foler
        /// </summary>
        public static IEnumerable<WIN32_FIND_DATA> GetFileSystemEntries(string path, string searchPattern, bool includeFiles, bool includeDirectories)
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

            if (IncludeInOutput(lpFindFileData.cFileName, lpFindFileData.dwFileAttributes, includeFiles, includeDirectories))
            {
                yield return lpFindFileData;
            }

            while (FindNextFile(handle, out lpFindFileData) != IntPtr.Zero)
            {
                if (IncludeInOutput(lpFindFileData.cFileName, lpFindFileData.dwFileAttributes, includeFiles, includeDirectories))
                {
                    lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);
                    yield return lpFindFileData;
                }
            }

            FindClose(handle);
        }

        private static bool IncludeInOutput(string cFileName, FileAttributes attributes, bool includeFiles, bool includeDirectories)
        {
            if (cFileName.Equals(".", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (cFileName.Equals("..", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!includeFiles && !attributes.HasFlag(FileAttributes.Directory))
            {
                return false;
            }

            if (!includeDirectories && attributes.HasFlag(FileAttributes.Directory))
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

        private const char SpaceChar = ' ';
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        
        /// <summary>
        /// Takes a filename and removes invalid characters
        /// </summary>
        public static string GetValidFilename(string filename)
        {
            foreach (char c in InvalidFileNameChars)
            {
                filename = filename.Replace(c, SpaceChar);
            }

            return filename;
        }
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

        public DateTime CreationTimeUtc
        {
            get
            {
                return ParseFileTime(ftCreationTime);
            }
        }

        public DateTime LastAccessTimeUtc
        {
            get
            {
                return ParseFileTime(ftLastAccessTime);
            }
        }

        public DateTime LastWriteTimeUtc
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
            return DateTime.FromFileTimeUtc(highBits + (long)filetime.dwLowDateTime);
        }

        public string Path { get; set; }
    }

}
