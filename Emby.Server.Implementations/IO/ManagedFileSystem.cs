using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.IO
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
        private bool EnableSeparateFileAndDirectoryQueries;

        private string _tempPath;

        private IEnvironmentInfo _environmentInfo;
        private bool _isEnvironmentCaseInsensitive;

        private string _defaultDirectory;

        public ManagedFileSystem(ILogger logger, IEnvironmentInfo environmentInfo, string defaultDirectory, string tempPath, bool enableSeparateFileAndDirectoryQueries)
        {
            Logger = logger;
            _supportsAsyncFileStreams = true;
            _tempPath = tempPath;
            _environmentInfo = environmentInfo;
            _defaultDirectory = defaultDirectory;

            // On Linux with mono, this needs to be true or symbolic links are ignored
            EnableSeparateFileAndDirectoryQueries = enableSeparateFileAndDirectoryQueries;

            SetInvalidFileNameChars(environmentInfo.OperatingSystem == MediaBrowser.Model.System.OperatingSystem.Windows);

            _isEnvironmentCaseInsensitive = environmentInfo.OperatingSystem == MediaBrowser.Model.System.OperatingSystem.Windows;
        }

        public string DefaultDirectory
        {
            get
            {
                var value = _defaultDirectory;

                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (DirectoryExists(value))
                        {
                            return value;
                        }
                    }
                    catch
                    {

                    }
                }

                return null;
            }
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
                // Be consistent across platforms because the windows server will fail to query network shares that don't follow windows conventions
                // https://referencesource.microsoft.com/#mscorlib/system/io/path.cs
                _invalidFileNameChars = new char[] { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, ':', '*', '?', '\\', '/' };
            }
        }

        public char DirectorySeparatorChar
        {
            get
            {
                return Path.DirectorySeparatorChar;
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

        public string MakeAbsolutePath(string folderPath, string filePath)
        {
            if (String.IsNullOrWhiteSpace(filePath)) return filePath;

            if (filePath.Contains(@"://")) return filePath; //stream
            if (filePath.Length > 3 && filePath[1] == ':' && filePath[2] == '/') return filePath; //absolute local path

            // unc path
            if (filePath.StartsWith("\\\\"))
            {
                return filePath;
            }

            var firstChar = filePath[0];
            if (firstChar == '/')
            {
                // For this we don't really know. 
                return filePath;
            }
            if (firstChar == '\\') //relative path
            {
                filePath = filePath.Substring(1);
            }
            try
            {
                string path = System.IO.Path.Combine(folderPath, filePath);
                path = System.IO.Path.GetFullPath(path);
                return path;
            }
            catch (ArgumentException)
            {
                return filePath;
            }
            catch (PathTooLongException)
            {
                return filePath;
            }
            catch (NotSupportedException)
            {
                return filePath;
            }
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
                result.IsDirectory = info is DirectoryInfo || (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                //if (!result.IsDirectory)
                //{
                //    result.IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                //}

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

        private ExtendedFileSystemInfo GetExtendedFileSystemInfo(string path)
        {
            var result = new ExtendedFileSystemInfo();

            var info = new FileInfo(path);

            if (info.Exists)
            {
                result.Exists = true;

                var attributes = info.Attributes;

                result.IsHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                result.IsReadOnly = (attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
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
                Logger.LogError(ex, "Error determining CreationTimeUtc for {FullName}", info.FullName);
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
                Logger.LogError(ex, "Error determining LastAccessTimeUtc for {FullName}", info.FullName);
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
                return GetFileStream(path, mode, access, share, FileOpenOptions.Asynchronous);
            }

            return GetFileStream(path, mode, access, share, FileOpenOptions.None);
        }

        public Stream GetFileStream(string path, FileOpenMode mode, FileAccessMode access, FileShareMode share, FileOpenOptions fileOpenOptions)
        {
            var defaultBufferSize = 4096;
            return new FileStream(path, GetFileMode(mode), GetFileAccess(access), GetFileShare(share), defaultBufferSize, GetFileOptions(fileOpenOptions));
        }

        private FileOptions GetFileOptions(FileOpenOptions mode)
        {
            var val = (int)mode;
            return (FileOptions)val;
        }

        private FileMode GetFileMode(FileOpenMode mode)
        {
            switch (mode)
            {
                //case FileOpenMode.Append:
                //    return FileMode.Append;
                case FileOpenMode.Create:
                    return FileMode.Create;
                case FileOpenMode.CreateNew:
                    return FileMode.CreateNew;
                case FileOpenMode.Open:
                    return FileMode.Open;
                case FileOpenMode.OpenOrCreate:
                    return FileMode.OpenOrCreate;
                //case FileOpenMode.Truncate:
                //    return FileMode.Truncate;
                default:
                    throw new Exception("Unrecognized FileOpenMode");
            }
        }

        private FileAccess GetFileAccess(FileAccessMode mode)
        {
            switch (mode)
            {
                //case FileAccessMode.ReadWrite:
                //    return FileAccess.ReadWrite;
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
            if (_environmentInfo.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows)
            {
                return;
            }

            var info = GetExtendedFileSystemInfo(path);

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
            if (_environmentInfo.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows)
            {
                return;
            }

            var info = GetExtendedFileSystemInfo(path);

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

        public void SetAttributes(string path, bool isHidden, bool isReadOnly)
        {
            if (_environmentInfo.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows)
            {
                return;
            }

            var info = GetExtendedFileSystemInfo(path);

            if (!info.Exists)
            {
                return;
            }

            if (info.IsReadOnly == isReadOnly && info.IsHidden == isHidden)
            {
                return;
            }

            var attributes = File.GetAttributes(path);

            if (isReadOnly)
            {
                attributes = attributes | FileAttributes.ReadOnly;
            }
            else
            {
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
            }

            if (isHidden)
            {
                attributes = attributes | FileAttributes.Hidden;
            }
            else
            {
                attributes = RemoveAttribute(attributes, FileAttributes.Hidden);
            }

            File.SetAttributes(path, attributes);
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

            var temp1 = Path.Combine(_tempPath, Guid.NewGuid().ToString("N"));

            // Copying over will fail against hidden files
            SetHidden(file1, false);
            SetHidden(file2, false);

            Directory.CreateDirectory(_tempPath);
            CopyFile(file1, temp1, true);

            CopyFile(file2, file1, true);
            CopyFile(temp1, file2, true);
        }

        private char GetDirectorySeparatorChar(string path)
        {
            return Path.DirectorySeparatorChar;
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

            var separatorChar = GetDirectorySeparatorChar(parentPath);

            return path.IndexOf(parentPath.TrimEnd(separatorChar) + separatorChar, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public bool IsRootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            var parent = GetDirectoryName(path);

            if (!string.IsNullOrEmpty(parent))
            {
                return false;
            }

            return true;
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
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

            return path.TrimEnd(GetDirectorySeparatorChar(path));
        }

        public bool AreEqual(string path1, string path2)
        {
            if (path1 == null && path2 == null)
            {
                return true;
            }

            if (path1 == null || path2 == null)
            {
                return false;
            }

            return string.Equals(NormalizePath(path1), NormalizePath(path2), StringComparison.OrdinalIgnoreCase);
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
            SetAttributes(path, false, false);
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

            return ToMetadata(new DirectoryInfo(path).EnumerateDirectories("*", searchOption));
        }

        public IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false)
        {
            return GetFiles(path, null, false, recursive);
        }

        public IEnumerable<FileSystemMetadata> GetFiles(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // On linux and osx the search pattern is case sensitive
            // If we're OK with case-sensitivity, and we're only filtering for one extension, then use the native method
            if ((enableCaseSensitiveExtensions || _isEnvironmentCaseInsensitive) && extensions != null && extensions.Length == 1)
            {
                return ToMetadata(new DirectoryInfo(path).EnumerateFiles("*" + extensions[0], searchOption));
            }

            var files = new DirectoryInfo(path).EnumerateFiles("*", searchOption);

            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(i =>
                {
                    var ext = i.Extension;
                    if (ext == null)
                    {
                        return false;
                    }
                    return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                });
            }

            return ToMetadata(files);
        }

        public IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false)
        {
            var directoryInfo = new DirectoryInfo(path);
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (EnableSeparateFileAndDirectoryQueries)
            {
                return ToMetadata(directoryInfo.EnumerateDirectories("*", searchOption))
                                .Concat(ToMetadata(directoryInfo.EnumerateFiles("*", searchOption)));
            }

            return ToMetadata(directoryInfo.EnumerateFileSystemInfos("*", searchOption));
        }

        private IEnumerable<FileSystemMetadata> ToMetadata(IEnumerable<FileSystemInfo> infos)
        {
            return infos.Select(GetFileSystemMetadata);
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

        private void CopyFileUsingStreams(string source, string target, bool overwrite)
        {
            using (var sourceStream = OpenRead(source))
            {
                using (var targetStream = GetFileStream(target, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                {
                    sourceStream.CopyTo(targetStream);
                }
            }
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
            return GetFilePaths(path, null, false, recursive);
        }

        public IEnumerable<string> GetFilePaths(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // On linux and osx the search pattern is case sensitive
            // If we're OK with case-sensitivity, and we're only filtering for one extension, then use the native method
            if ((enableCaseSensitiveExtensions || _isEnvironmentCaseInsensitive) && extensions != null && extensions.Length == 1)
            {
                return Directory.EnumerateFiles(path, "*" + extensions[0], searchOption);
            }

            var files = Directory.EnumerateFiles(path, "*", searchOption);

            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(i =>
                {
                    var ext = Path.GetExtension(i);
                    if (ext == null)
                    {
                        return false;
                    }
                    return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                });
            }

            return files;
        }

        public IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFileSystemEntries(path, "*", searchOption);
        }

        public virtual void SetExecutable(string path)
        {
            if (_environmentInfo.OperatingSystem == MediaBrowser.Model.System.OperatingSystem.OSX)
            {
                RunProcess("chmod", "+x \"" + path + "\"", GetDirectoryName(path));
            }
        }

        private void RunProcess(string path, string args, string workingDirectory)
        {
            using (var process = Process.Start(new ProcessStartInfo
            {
                Arguments = args,
                FileName = path,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                WindowStyle = ProcessWindowStyle.Normal
            }))
            {
                process.WaitForExit();
            }
        }
    }
}
