using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Text;

namespace MediaBrowser.Common.Implementations.IO
{
    /// <summary>
    /// Class CommonFileSystem
    /// </summary>
    public class CommonFileSystem : IFileSystem
    {
        protected ILogger Logger;

        private readonly bool _supportsAsyncFileStreams;

        public CommonFileSystem(ILogger logger, bool supportsAsyncFileStreams)
        {
            Logger = logger;
            _supportsAsyncFileStreams = supportsAsyncFileStreams;
        }

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public virtual bool IsShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            var extension = Path.GetExtension(filename);

            return string.Equals(extension, ".mblink", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves the shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public virtual string ResolveShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            if (string.Equals(Path.GetExtension(filename), ".mblink", StringComparison.OrdinalIgnoreCase))
            {
                return File.ReadAllText(filename);
            }

            return null;
        }

        /// <summary>
        /// Creates the shortcut.
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">
        /// shortcutPath
        /// or
        /// target
        /// </exception>
        public void CreateShortcut(string shortcutPath, string target)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException("shortcutPath");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException("target");
            }

            File.WriteAllText(shortcutPath, target);
        }

        /// <summary>
        /// Gets the file system info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        public FileSystemInfo GetFileSystemInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

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
        /// The space char
        /// </summary>
        private const char SpaceChar = ' ';
        /// <summary>
        /// The invalid file name chars
        /// </summary>
        #if __MonoCS__
        private static readonly char[] InvalidFileNameChars = new char [41] { '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
            '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F', '\x10', '\x11', '\x12',
            '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D',
            '\x1E', '\x1F', '\x22', '\x3C', '\x3E', '\x7C', ':', '*', '?', '\\', '/' };
        #else
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        #endif

        /// <summary>
        /// Takes a filename and removes invalid characters
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">filename</exception>
        public string GetValidFilename(string filename)
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
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>DateTime.</returns>
        public DateTime GetCreationTimeUtc(FileSystemInfo info)
        {
            // This could throw an error on some file systems that have dates out of range
            try
            {
                return info.CreationTimeUtc;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error determining CreationTimeUtc for {0}", ex, info.FullName);
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>DateTime.</returns>
        public DateTime GetLastWriteTimeUtc(FileSystemInfo info)
        {
            // This could throw an error on some file systems that have dates out of range
            try
            {
                return info.LastWriteTimeUtc;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error determining LastAccessTimeUtc for {0}", ex, info.FullName);
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        public DateTime GetLastWriteTimeUtc(string path)
        {
            return GetLastWriteTimeUtc(GetFileSystemInfo(path));
        }

        /// <summary>
        /// Gets the file stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <param name="isAsync">if set to <c>true</c> [is asynchronous].</param>
        /// <returns>FileStream.</returns>
        public FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share, bool isAsync = false)
        {
            if (_supportsAsyncFileStreams && isAsync)
            {
                return new FileStream(path, mode, access, share, 4096, true);
            }

            return new FileStream(path, mode, access, share);
        }

        /// <summary>
        /// Swaps the files.
        /// </summary>
        /// <param name="file1">The file1.</param>
        /// <param name="file2">The file2.</param>
        public void SwapFiles(string file1, string file2)
        {
            if (string.IsNullOrEmpty(file1))
            {
                throw new ArgumentNullException("file1");
            }

            if (string.IsNullOrEmpty(file2))
            {
                throw new ArgumentNullException("file2");
            }

            var temp1 = Path.GetTempFileName();
            var temp2 = Path.GetTempFileName();

            // Copying over will fail against hidden files
            RemoveHiddenAttribute(file1);
            RemoveHiddenAttribute(file2);

            File.Copy(file1, temp1, true);
            File.Copy(file2, temp2, true);

            File.Copy(temp1, file2, true);
            File.Copy(temp2, file1, true);

            File.Delete(temp1);
            File.Delete(temp2);
        }

        /// <summary>
        /// Removes the hidden attribute.
        /// </summary>
        /// <param name="path">The path.</param>
        private void RemoveHiddenAttribute(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var currentFile = new FileInfo(path);

            // This will fail if the file is hidden
            if (currentFile.Exists)
            {
                if ((currentFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    currentFile.Attributes &= ~FileAttributes.Hidden;
                }
            }
        }

        public bool ContainsSubPath(string parentPath, string path)
        {
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new ArgumentNullException("parentPath");
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            
            return path.IndexOf(parentPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public bool IsRootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            
            var parent = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(parent))
            {
                return false;   
            }

            return true;
        }

        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (path.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path.TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}
