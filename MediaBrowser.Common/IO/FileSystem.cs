using MediaBrowser.Common.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MediaBrowser.Common.IO
{
    /// <summary>
    /// Class FileSystem
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Gets information about a path
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{WIN32_FIND_DATA}.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        /// <exception cref="System.IO.IOException">GetFileData failed for  + path</exception>
        public static WIN32_FIND_DATA? GetFileData(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            WIN32_FIND_DATA data;
            var handle = NativeMethods.FindFirstFileEx(path, FINDEX_INFO_LEVELS.FindExInfoBasic, out data,
                                          FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FindFirstFileExFlags.NONE);

            var getFilename = false;

            if (handle == NativeMethods.INVALID_HANDLE_VALUE && !Path.HasExtension(path))
            {
                if (!path.EndsWith("*", StringComparison.OrdinalIgnoreCase))
                {
                    NativeMethods.FindClose(handle);

                    handle = NativeMethods.FindFirstFileEx(Path.Combine(path, "*"), FINDEX_INFO_LEVELS.FindExInfoBasic, out data,
                                          FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FindFirstFileExFlags.NONE);

                    getFilename = true;
                }
            }

            if (handle == IntPtr.Zero)
            {
                throw new IOException("GetFileData failed for " + path);
            }

            NativeMethods.FindClose(handle);

            // According to MSDN documentation, this will default to 1601 for paths that don't exist.		
            if (data.CreationTimeUtc.Year == 1601)
            {
                return null;
            }

            if (getFilename)
            {
                data.cFileName = Path.GetFileName(path);
            }

            data.Path = path;
            return data;
        }

        /// <summary>
        /// Gets all files within a folder
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <returns>IEnumerable{WIN32_FIND_DATA}.</returns>
        public static IEnumerable<WIN32_FIND_DATA> GetFiles(string path, string searchPattern = "*")
        {
            return GetFileSystemEntries(path, searchPattern, includeDirectories: false);
        }

        /// <summary>
        /// Gets all sub-directories within a folder
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{WIN32_FIND_DATA}.</returns>
        public static IEnumerable<WIN32_FIND_DATA> GetDirectories(string path)
        {
            return GetFileSystemEntries(path, includeFiles: false);
        }

        /// <summary>
        /// Gets all file system entries within a foler
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="searchPattern">The search pattern.</param>
        /// <param name="includeFiles">if set to <c>true</c> [include files].</param>
        /// <param name="includeDirectories">if set to <c>true</c> [include directories].</param>
        /// <returns>IEnumerable{WIN32_FIND_DATA}.</returns>
        /// <exception cref="System.ArgumentNullException">path</exception>
        /// <exception cref="System.IO.IOException">GetFileSystemEntries failed</exception>
        public static IEnumerable<WIN32_FIND_DATA> GetFileSystemEntries(string path, string searchPattern = "*", bool includeFiles = true, bool includeDirectories = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            
            var lpFileName = Path.Combine(path, searchPattern);

            WIN32_FIND_DATA lpFindFileData;
            var handle = NativeMethods.FindFirstFileEx(lpFileName, FINDEX_INFO_LEVELS.FindExInfoBasic, out lpFindFileData,
                                          FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, FindFirstFileExFlags.FIND_FIRST_EX_LARGE_FETCH);

            if (handle == IntPtr.Zero)
            {
                var hr = Marshal.GetLastWin32Error();
                if (hr != 2 && hr != 0x12)
                {
                    throw new IOException("GetFileSystemEntries failed");
                }
                yield break;
            }

            if (IncludeInFindFileOutput(lpFindFileData.cFileName, lpFindFileData.dwFileAttributes, includeFiles, includeDirectories))
            {
                lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);

                yield return lpFindFileData;
            }

            while (NativeMethods.FindNextFile(handle, out lpFindFileData) != IntPtr.Zero)
            {
                if (IncludeInFindFileOutput(lpFindFileData.cFileName, lpFindFileData.dwFileAttributes, includeFiles, includeDirectories))
                {
                    lpFindFileData.Path = Path.Combine(path, lpFindFileData.cFileName);
                    yield return lpFindFileData;
                }
            }

            NativeMethods.FindClose(handle);
        }

        /// <summary>
        /// Includes the in find file output.
        /// </summary>
        /// <param name="cFileName">Name of the c file.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="includeFiles">if set to <c>true</c> [include files].</param>
        /// <param name="includeDirectories">if set to <c>true</c> [include directories].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public static bool IncludeInFindFileOutput(string cFileName, FileAttributes attributes, bool includeFiles, bool includeDirectories)
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

        /// <summary>
        /// The space char
        /// </summary>
        private const char SpaceChar = ' ';
        /// <summary>
        /// The invalid file name chars
        /// </summary>
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Takes a filename and removes invalid characters
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public static string GetValidFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            
            foreach (var c in InvalidFileNameChars)
            {
                filename = filename.Replace(c, SpaceChar);
            }

            return filename;
        }

        /// <summary>
        /// Resolves the shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public static string ResolveShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            
            var link = new ShellLink();
            ((IPersistFile)link).Load(filename, NativeMethods.STGM_READ);
            // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
            // ((IShellLinkW)link).Resolve(hwnd, 0) 
            var sb = new StringBuilder(NativeMethods.MAX_PATH);
            WIN32_FIND_DATA data;
            ((IShellLinkW)link).GetPath(sb, sb.Capacity, out data, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Creates a shortcut file pointing to a specified path
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">shortcutPath</exception>
        public static void CreateShortcut(string shortcutPath, string target)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException("shortcutPath");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }
            
            var link = new ShellLink();

            ((IShellLinkW)link).SetPath(target);

            ((IPersistFile)link).Save(shortcutPath, true);
        }

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public static bool IsShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            
            return string.Equals(Path.GetExtension(filename), ".lnk", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Copies all.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">source</exception>
        /// <exception cref="System.ArgumentException">The source and target directories are the same</exception>
        public static void CopyAll(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }
            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The source and target directories are the same");
            }

            // Check if the target directory exists, if not, create it. 
            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
            }

            foreach (var file in Directory.EnumerateFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }

            // Copy each subdirectory using recursion. 
            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                CopyAll(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }

        /// <summary>
        /// Parses the ini file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>NameValueCollection.</returns>
        public static NameValueCollection ParseIniFile(string path)
        {
            var values = new NameValueCollection();

            foreach (var line in File.ReadAllLines(path))
            {
                var data = line.Split('=');

                if (data.Length < 2) continue;

                var key = data[0];

                var value = data.Length == 2 ? data[1] : string.Join(string.Empty, data, 1, data.Length - 1);

                values[key] = value;
            }

            return values;
        }
    }
}
