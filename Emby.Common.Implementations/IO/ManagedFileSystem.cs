using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;

namespace Emby.Common.Implementations.IO
{
    /// <summary>
    /// Class ManagedFileSystem
    /// </summary>
    public class ManagedFileSystem : IFileSystem
    {
        protected ILogger Logger;

        private readonly bool _supportsAsyncFileStreams;
        private char[] _invalidFileNameChars;
        private readonly List<IShortcutHandler> _shortcutHandlers = new List<IShortcutHandler>();
        private bool EnableFileSystemRequestConcat = true;

        public ManagedFileSystem(ILogger logger, bool supportsAsyncFileStreams, bool enableManagedInvalidFileNameChars, bool enableFileSystemRequestConcat)
        {
            Logger = logger;
            _supportsAsyncFileStreams = supportsAsyncFileStreams;
            EnableFileSystemRequestConcat = enableFileSystemRequestConcat;
            SetInvalidFileNameChars(enableManagedInvalidFileNameChars);
        }

        public void AddShortcutHandler(IShortcutHandler handler)
        {
            _shortcutHandlers.Add(handler);
        }

        protected void SetInvalidFileNameChars(bool enableManagedInvalidFileNameChars)
        {
            if (enableManagedInvalidFileNameChars)
            {
                _invalidFileNameChars = Path.GetInvalidFileNameChars();
            }
            else
            {
                // GetInvalidFileNameChars is less restrictive in Linux/Mac than Windows, this mimic Windows behavior for mono under Linux/Mac.
                _invalidFileNameChars = new char[41] { '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
            '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F', '\x10', '\x11', '\x12',
            '\x13', '\x14', '\x15', '\x16', '\x17', '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D',
            '\x1E', '\x1F', '\x22', '\x3C', '\x3E', '\x7C', ':', '*', '?', '\\', '/' };
            }
        }

        public char DirectorySeparatorChar
        {
            get
            {
                return Path.DirectorySeparatorChar;
            }
        }

        public char PathSeparator
        {
            get
            {
                return Path.PathSeparator;
            }
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
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
            return _shortcutHandlers.Any(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));
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

            var extension = Path.GetExtension(filename);
            var handler = _shortcutHandlers.FirstOrDefault(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));

            if (handler != null)
            {
                return handler.Resolve(filename);
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

            var extension = Path.GetExtension(shortcutPath);
            var handler = _shortcutHandlers.FirstOrDefault(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));

            if (handler != null)
            {
                handler.Create(shortcutPath, target);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified file or directory path.
        /// </summary>
        /// <param name="path">A path to a file or directory.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks>If the specified path points to a directory, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property will be set to true and all other properties will reflect the properties of the directory.</remarks>
        public FileSystemMetadata GetFileSystemInfo(string path)
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
                    return GetFileSystemMetadata(fileInfo);
                }

                return GetFileSystemMetadata(new DirectoryInfo(path));
            }
            else
            {
                var fileInfo = new DirectoryInfo(path);

                if (fileInfo.Exists)
                {
                    return GetFileSystemMetadata(fileInfo);
                }

                return GetFileSystemMetadata(new FileInfo(path));
            }
        }

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified file path.
        /// </summary>
        /// <param name="path">A path to a file.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks><para>If the specified path points to a directory, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property and the <see cref="FileSystemMetadata.Exists"/> property will both be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="GetFileSystemInfo"/>.</para></remarks>
        public FileSystemMetadata GetFileInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var fileInfo = new FileInfo(path);

            return GetFileSystemMetadata(fileInfo);
        }

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified directory path.
        /// </summary>
        /// <param name="path">A path to a directory.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks><para>If the specified path points to a file, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property will be set to true and the <see cref="FileSystemMetadata.Exists"/> property will be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="GetFileSystemInfo"/>.</para></remarks>
        public FileSystemMetadata GetDirectoryInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var fileInfo = new DirectoryInfo(path);

            return GetFileSystemMetadata(fileInfo);
        }

        private FileSystemMetadata GetFileSystemMetadata(FileSystemInfo info)
        {
            var result = new FileSystemMetadata();

            result.Exists = info.Exists;
            result.FullName = info.FullName;
            result.Extension = info.Extension;
            result.Name = info.Name;

            if (result.Exists)
            {
                var attributes = info.Attributes;
                result.IsDirectory = info is DirectoryInfo || (attributes & FileAttributes.Directory) == FileAttributes.Directory;
                result.IsHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                result.IsReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

                var fileInfo = info as FileInfo;
                if (fileInfo != null)
                {
                    result.Length = fileInfo.Length;
                    result.DirectoryName = fileInfo.DirectoryName;
                }

                result.CreationTimeUtc = GetCreationTimeUtc(info);
                result.LastWriteTimeUtc = GetLastWriteTimeUtc(info);
            }
            else
            {
                result.IsDirectory = info is DirectoryInfo;
            }

            return result;
        }

        /// <summary>
        /// The space char
        /// </summary>
        private const char SpaceChar = ' ';

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

            foreach (var c in _invalidFileNameChars)
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
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        public DateTime GetCreationTimeUtc(string path)
        {
            return GetCreationTimeUtc(GetFileSystemInfo(path));
        }

        public DateTime GetCreationTimeUtc(FileSystemMetadata info)
        {
            return info.CreationTimeUtc;
        }

        public DateTime GetLastWriteTimeUtc(FileSystemMetadata info)
        {
            return info.LastWriteTimeUtc;
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
        public Stream GetFileStream(string path, FileOpenMode mode, FileAccessMode access, FileShareMode share, bool isAsync = false)
        {
            if (_supportsAsyncFileStreams && isAsync)
            {
                return new FileStream(path, GetFileMode(mode), GetFileAccess(access), GetFileShare(share), 262144, true);
            }

            return new FileStream(path, GetFileMode(mode), GetFileAccess(access), GetFileShare(share), 262144);
        }

        private FileMode GetFileMode(FileOpenMode mode)
        {
            switch (mode)
            {
                case FileOpenMode.Append:
                    return FileMode.Append;
                case FileOpenMode.Create:
                    return FileMode.Create;
                case FileOpenMode.CreateNew:
                    return FileMode.CreateNew;
                case FileOpenMode.Open:
                    return FileMode.Open;
                case FileOpenMode.OpenOrCreate:
                    return FileMode.OpenOrCreate;
                case FileOpenMode.Truncate:
                    return FileMode.Truncate;
                default:
                    throw new Exception("Unrecognized FileOpenMode");
            }
        }

        private FileAccess GetFileAccess(FileAccessMode mode)
        {
            switch (mode)
            {
                case FileAccessMode.ReadWrite:
                    return FileAccess.ReadWrite;
                case FileAccessMode.Write:
                    return FileAccess.Write;
                case FileAccessMode.Read:
                    return FileAccess.Read;
                default:
                    throw new Exception("Unrecognized FileAccessMode");
            }
        }

        private FileShare GetFileShare(FileShareMode mode)
        {
            switch (mode)
            {
                case FileShareMode.ReadWrite:
                    return FileShare.ReadWrite;
                case FileShareMode.Write:
                    return FileShare.Write;
                case FileShareMode.Read:
                    return FileShare.Read;
                case FileShareMode.None:
                    return FileShare.None;
                default:
                    throw new Exception("Unrecognized FileShareMode");
            }
        }

        public void SetHidden(string path, bool isHidden)
        {
            var info = GetFileInfo(path);

            if (info.Exists && info.IsHidden != isHidden)
            {
                if (isHidden)
                {
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    attributes = RemoveAttribute(attributes, FileAttributes.Hidden);
                    File.SetAttributes(path, attributes);
                }
            }
        }

        public void SetReadOnly(string path, bool isReadOnly)
        {
            var info = GetFileInfo(path);

            if (info.Exists && info.IsReadOnly != isReadOnly)
            {
                if (isReadOnly)
                {
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                }
            }
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
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

            CopyFile(file1, temp1, true);
            CopyFile(file2, temp2, true);

            CopyFile(temp1, file2, true);
            CopyFile(temp2, file1, true);

            DeleteFile(temp1);
            DeleteFile(temp2);
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

        public string GetFileNameWithoutExtension(FileSystemMetadata info)
        {
            if (info.IsDirectory)
            {
                return info.Name;
            }

            return Path.GetFileNameWithoutExtension(info.FullName);
        }

        public string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public bool IsPathFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            // Cannot use Path.IsPathRooted because it returns false under mono when using windows-based paths, e.g. C:\\

            if (path.IndexOf("://", StringComparison.OrdinalIgnoreCase) != -1 &&
                !path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;

            //return Path.IsPathRooted(path);
        }

        public void DeleteFile(string path)
        {
            var fileInfo = GetFileInfo(path);

            if (fileInfo.Exists)
            {
                if (fileInfo.IsHidden)
                {
                    SetHidden(path, false);
                }
                if (fileInfo.IsReadOnly)
                {
                    SetReadOnly(path, false);
                }
            }

            File.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public List<FileSystemMetadata> GetDrives()
        {
            // Only include drives in the ready state or this method could end up being very slow, waiting for drives to timeout
            return DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => new FileSystemMetadata
            {
                Name = GetName(d),
                FullName = d.RootDirectory.FullName,
                IsDirectory = true

            }).ToList();
        }

        private string GetName(DriveInfo drive)
        {
            return drive.Name;
        }

        public IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return ToMetadata(path, new DirectoryInfo(path).EnumerateDirectories("*", searchOption));
        }

        public IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return ToMetadata(path, new DirectoryInfo(path).EnumerateFiles("*", searchOption));
        }

        public IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false)
        {
            var directoryInfo = new DirectoryInfo(path);
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (EnableFileSystemRequestConcat)
            {
                return ToMetadata(path, directoryInfo.EnumerateDirectories("*", searchOption))
                                .Concat(ToMetadata(path, directoryInfo.EnumerateFiles("*", searchOption)));
            }

            return ToMetadata(path, directoryInfo.EnumerateFileSystemInfos("*", searchOption));
        }

        private IEnumerable<FileSystemMetadata> ToMetadata(string parentPath, IEnumerable<FileSystemInfo> infos)
        {
            return infos.Select(i =>
            {
                try
                {
                    return GetFileSystemMetadata(i);
                }
                catch (PathTooLongException)
                {
                    // Can't log using the FullName because it will throw the PathTooLongExceptiona again
                    //Logger.Warn("Path too long: {0}", i.FullName);
                    Logger.Warn("File or directory path too long. Parent folder: {0}", parentPath);
                    return null;
                }

            }).Where(i => i != null);
        }

        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            File.WriteAllLines(path, lines);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public void CopyFile(string source, string target, bool overwrite)
        {
            File.Copy(source, target, overwrite);
        }

        public void MoveFile(string source, string target)
        {
            File.Move(source, target);
        }

        public void MoveDirectory(string source, string target)
        {
            Directory.Move(source, target);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public void WriteAllText(string path, string text, Encoding encoding)
        {
            File.WriteAllText(path, text, encoding);
        }

        public void WriteAllText(string path, string text)
        {
            File.WriteAllText(path, text);
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            return File.ReadAllText(path, encoding);
        }

        public IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateDirectories(path, "*", searchOption);
        }

        public IEnumerable<string> GetFilePaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFiles(path, "*", searchOption);
        }

        public IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFileSystemEntries(path, "*", searchOption);
        }

        public virtual void SetExecutable(string path)
        {
            
        }
    }
}
