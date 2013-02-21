using MediaBrowser.Common.IO;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Win32;
using MediaBrowser.Controller.Library;
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
        /// <summary>
        /// Gets all file system entries within a foler
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="includeFiles">if set to <c>true</c> [include files].</param>
        /// <param name="includeDirectories">if set to <c>true</c> [include directories].</param>
        /// <param name="flattenFolderDepth">The flatten folder depth.</param>
        /// <param name="args">The args.</param>
        /// <returns>Dictionary{System.StringWIN32_FIND_DATA}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.IO.IOException">GetFileSystemEntries failed</exception>
        public static Dictionary<string, WIN32_FIND_DATA> GetFilteredFileSystemEntries(string path, string searchPattern = "*", bool includeFiles = true, bool includeDirectories = true, int flattenFolderDepth = 0, ItemResolveArgs args = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            var lpFileName = Path.Combine(path, searchPattern);

            WIN32_FIND_DATA lpFindFileData;
            var handle = NativeMethods.FindFirstFileEx(lpFileName, FINDEX_INFO_LEVELS.FindExInfoBasic, out lpFindFileData,
                                          FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FindFirstFileExFlags.FIND_FIRST_EX_LARGE_FETCH);

            if (handle == IntPtr.Zero)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr != 2 && hr != 0x12)
                {
                    throw new IOException("GetFileSystemEntries failed");
                }
                return new Dictionary<string, WIN32_FIND_DATA>(StringComparer.OrdinalIgnoreCase);
            }

            var dict = new Dictionary<string, WIN32_FIND_DATA>(StringComparer.OrdinalIgnoreCase);

            if (FileSystem.IncludeInFindFileOutput(lpFindFileData.cFileName, lpFindFileData.dwFileAttributes, includeFiles, includeDirectories))
            {
                if (!string.IsNullOrEmpty(lpFindFileData.cFileName))
                {
                    lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);

                    dict[lpFindFileData.Path] = lpFindFileData;
                }
            }

            while (NativeMethods.FindNextFile(handle, out lpFindFileData) != IntPtr.Zero)
            {
                // This is the one circumstance where we can completely disregard a file
                if (lpFindFileData.IsSystemFile)
                {
                    continue;
                }

                // Filter out invalid entries
                if (lpFindFileData.cFileName.Equals(".", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (lpFindFileData.cFileName.Equals("..", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);

                if (FileSystem.IsShortcut(lpFindFileData.Path))
                {
                    var newPath = FileSystem.ResolveShortcut(lpFindFileData.Path);
                    if (string.IsNullOrWhiteSpace(newPath))
                    {
                        //invalid shortcut - could be old or target could just be unavailable
                        Logger.LogWarning("Encountered invalid shortuct: "+lpFindFileData.Path);
                        continue;
                    }
                    var data = FileSystem.GetFileData(newPath);

                    if (data.HasValue)
                    {
                        lpFindFileData = data.Value;

                        // Find out if the shortcut is pointing to a directory or file
                        if (lpFindFileData.IsDirectory)
                        {
                            // add to our physical locations
                            if (args != null)
                            {
                                args.AddAdditionalLocation(newPath);
                            }
                        }

                        dict[lpFindFileData.Path] = lpFindFileData;
                    }
                }
                else if (flattenFolderDepth > 0 && lpFindFileData.IsDirectory)
                {
                    foreach (var child in GetFilteredFileSystemEntries(lpFindFileData.Path, flattenFolderDepth: flattenFolderDepth - 1))
                    {
                        dict[child.Key] = child.Value;
                    }
                }
                else
                {
                    dict[lpFindFileData.Path] = lpFindFileData;
                }
            }

            NativeMethods.FindClose(handle);
            return dict;
        }
    }

}
