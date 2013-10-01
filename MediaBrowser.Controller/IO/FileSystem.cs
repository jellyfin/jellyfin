using MediaBrowser.Model.Logging;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// Class FileSystem
    /// </summary>
    public static class FileSystem
    {
        /// <summary>
        /// Gets the file system info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        public static FileSystemInfo GetFileSystemInfo(string path)
        {
            // Take a guess to try and avoid two file system hits, but we'll double-check by calling Exists
            if (Path.HasExtension(path))
            {
                var fileInfo = new FileInfo(path);

                if (fileInfo.Exists)
                {
                    return fileInfo;
                }

                return new DirectoryInfo(path);
            }
            else
            {
                var fileInfo = new DirectoryInfo(path);

                if (fileInfo.Exists)
                {
                    return fileInfo;
                }

                return new FileInfo(path);
            }
        }

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>DateTime.</returns>
        public static DateTime GetLastWriteTimeUtc(FileSystemInfo info, ILogger logger)
        {
            // This could throw an error on some file systems that have dates out of range

            try
            {
                return info.LastWriteTimeUtc;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error determining LastAccessTimeUtc for {0}", ex, info.FullName);
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>DateTime.</returns>
        public static DateTime GetCreationTimeUtc(FileSystemInfo info, ILogger logger)
        {
            // This could throw an error on some file systems that have dates out of range

            try
            {
                return info.CreationTimeUtc;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error determining CreationTimeUtc for {0}", ex, info.FullName);
                return DateTime.MinValue;
            }
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

            var builder = new StringBuilder(filename);

            foreach (var c in InvalidFileNameChars)
            {
                builder = builder.Replace(c, SpaceChar);
            }

            return builder.ToString();
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

            return new WindowsShortcut(filename).ResolvedPath;

            //var link = new ShellLink();
            //((IPersistFile)link).Load(filename, NativeMethods.STGM_READ);
            //// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
            //// ((IShellLinkW)link).Resolve(hwnd, 0) 
            //var sb = new StringBuilder(NativeMethods.MAX_PATH);
            //WIN32_FIND_DATA data;
            //((IShellLinkW)link).GetPath(sb, sb.Capacity, out data, 0);
            //return sb.ToString();
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
            Directory.CreateDirectory(target);

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

    public class WindowsShortcut
    {
        public bool IsDirectory { get; private set; }
        public bool IsLocal { get; private set; }
        public string ResolvedPath { get; private set; }

        public WindowsShortcut(string file)
        {
            ParseLink(File.ReadAllBytes(file));
        }

        private static bool isMagicPresent(byte[] link)
        {

            const int magic = 0x0000004C;
            const int magic_offset = 0x00;

            return link.Length >= 32 && bytesToDword(link, magic_offset) == magic;
        }

        /**
         * Gobbles up link data by parsing it and storing info in member fields
         * @param link all the bytes from the .lnk file
         */
        private void ParseLink(byte[] link)
        {
            if (!isMagicPresent(link))
                throw new IOException("Invalid shortcut; magic is missing", 0);

            // get the flags byte
            byte flags = link[0x14];

            // get the file attributes byte
            const int file_atts_offset = 0x18;
            byte file_atts = link[file_atts_offset];
            byte is_dir_mask = (byte)0x10;
            if ((file_atts & is_dir_mask) > 0)
            {
                IsDirectory = true;
            }
            else
            {
                IsDirectory = false;
            }

            // if the shell settings are present, skip them
            const int shell_offset = 0x4c;
            const byte has_shell_mask = (byte)0x01;
            int shell_len = 0;
            if ((flags & has_shell_mask) > 0)
            {
                // the plus 2 accounts for the length marker itself
                shell_len = bytesToWord(link, shell_offset) + 2;
            }

            // get to the file settings
            int file_start = 0x4c + shell_len;

            const int file_location_info_flag_offset_offset = 0x08;
            int file_location_info_flag = link[file_start + file_location_info_flag_offset_offset];
            IsLocal = (file_location_info_flag & 2) == 0;
            // get the local volume and local system values
            //final int localVolumeTable_offset_offset = 0x0C;
            const int basename_offset_offset = 0x10;
            const int networkVolumeTable_offset_offset = 0x14;
            const int finalname_offset_offset = 0x18;
            int finalname_offset = link[file_start + finalname_offset_offset] + file_start;
            String finalname = getNullDelimitedString(link, finalname_offset);
            if (IsLocal)
            {
                int basename_offset = link[file_start + basename_offset_offset] + file_start;
                String basename = getNullDelimitedString(link, basename_offset);
                ResolvedPath = basename + finalname;
            }
            else
            {
                int networkVolumeTable_offset = link[file_start + networkVolumeTable_offset_offset] + file_start;
                int shareName_offset_offset = 0x08;
                int shareName_offset = link[networkVolumeTable_offset + shareName_offset_offset]
                    + networkVolumeTable_offset;
                String shareName = getNullDelimitedString(link, shareName_offset);
                ResolvedPath = shareName + "\\" + finalname;
            }
        }

        private static string getNullDelimitedString(byte[] bytes, int off)
        {
            int len = 0;

            // count bytes until the null character (0)
            while (true)
            {
                if (bytes[off + len] == 0)
                {
                    break;
                }
                len++;
            }

            return Encoding.UTF8.GetString(bytes, off, len);
        }

        /*
         * convert two bytes into a short note, this is little endian because it's
         * for an Intel only OS.
         */
        private static int bytesToWord(byte[] bytes, int off)
        {
            return ((bytes[off + 1] & 0xff) << 8) | (bytes[off] & 0xff);
        }

        private static int bytesToDword(byte[] bytes, int off)
        {
            return (bytesToWord(bytes, off + 2) << 16) | bytesToWord(bytes, off);
        }

    }
}
