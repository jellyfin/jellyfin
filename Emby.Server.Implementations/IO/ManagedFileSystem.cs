#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;
using OperatingSystem = MediaBrowser.Common.System.OperatingSystem;

namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// Class ManagedFileSystem.
    /// </summary>
    public class ManagedFileSystem : IFileSystem
    {
        protected ILogger Logger;

        private readonly List<IShortcutHandler> _shortcutHandlers = new List<IShortcutHandler>();
        private readonly string _tempPath;
        private readonly bool _isEnvironmentCaseInsensitive;

        public ManagedFileSystem(
            ILogger<ManagedFileSystem> logger,
            IApplicationPaths applicationPaths)
        {
            Logger = logger;
            _tempPath = applicationPaths.TempDirectory;

            _isEnvironmentCaseInsensitive = OperatingSystem.Id == OperatingSystemId.Windows;
        }

        public virtual void AddShortcutHandler(IShortcutHandler handler)
        {
            _shortcutHandlers.Add(handler);
        }

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">filename</exception>
        public virtual bool IsShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var extension = Path.GetExtension(filename);
            return _shortcutHandlers.Any(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves the shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">filename</exception>
        public virtual string ResolveShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var extension = Path.GetExtension(filename);
            var handler = _shortcutHandlers.FirstOrDefault(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));

            return handler?.Resolve(filename);
        }

        public virtual string MakeAbsolutePath(string folderPath, string filePath)
        {
            // path is actually a stream
            if (string.IsNullOrWhiteSpace(filePath) || filePath.Contains("://", StringComparison.Ordinal))
            {
                return filePath;
            }

            if (filePath.Length > 3 && filePath[1] == ':' && filePath[2] == '/')
            {
                // absolute local path
                return filePath;
            }

            // unc path
            if (filePath.StartsWith("\\\\", StringComparison.Ordinal))
            {
                return filePath;
            }

            var firstChar = filePath[0];
            if (firstChar == '/')
            {
                // for this we don't really know
                return filePath;
            }

            // relative path
            if (firstChar == '\\')
            {
                filePath = filePath.Substring(1);
            }

            try
            {
                return Path.GetFullPath(Path.Combine(folderPath, filePath));
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
        /// <exception cref="ArgumentNullException">The shortcutPath or target is null.</exception>
        public virtual void CreateShortcut(string shortcutPath, string target)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException(nameof(shortcutPath));
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentNullException(nameof(target));
            }

            var extension = Path.GetExtension(shortcutPath);
            var handler = _shortcutHandlers.Find(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));

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
        public virtual FileSystemMetadata GetFileSystemInfo(string path)
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
        public virtual FileSystemMetadata GetFileInfo(string path)
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
        public virtual FileSystemMetadata GetDirectoryInfo(string path)
        {
            var fileInfo = new DirectoryInfo(path);

            return GetFileSystemMetadata(fileInfo);
        }

        private FileSystemMetadata GetFileSystemMetadata(FileSystemInfo info)
        {
            var result = new FileSystemMetadata
            {
                Exists = info.Exists,
                FullName = info.FullName,
                Extension = info.Extension,
                Name = info.Name
            };

            if (result.Exists)
            {
                result.IsDirectory = info is DirectoryInfo || (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                //if (!result.IsDirectory)
                //{
                //    result.IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                //}

                if (info is FileInfo fileInfo)
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

        private static ExtendedFileSystemInfo GetExtendedFileSystemInfo(string path)
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
        /// Takes a filename and removes invalid characters.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">The filename is null.</exception>
        public virtual string GetValidFilename(string filename)
        {
            var builder = new StringBuilder(filename);

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                builder = builder.Replace(c, ' ');
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
        public virtual DateTime GetCreationTimeUtc(string path)
        {
            return GetCreationTimeUtc(GetFileSystemInfo(path));
        }

        public virtual DateTime GetCreationTimeUtc(FileSystemMetadata info)
        {
            return info.CreationTimeUtc;
        }

        public virtual DateTime GetLastWriteTimeUtc(FileSystemMetadata info)
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
        public virtual DateTime GetLastWriteTimeUtc(string path)
        {
            return GetLastWriteTimeUtc(GetFileSystemInfo(path));
        }

        public virtual void SetHidden(string path, bool isHidden)
        {
            if (OperatingSystem.Id != OperatingSystemId.Windows)
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
                    var attributes = File.GetAttributes(path);
                    attributes = RemoveAttribute(attributes, FileAttributes.Hidden);
                    File.SetAttributes(path, attributes);
                }
            }
        }

        public virtual void SetReadOnly(string path, bool isReadOnly)
        {
            if (OperatingSystem.Id != OperatingSystemId.Windows)
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
                    var attributes = File.GetAttributes(path);
                    attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                    File.SetAttributes(path, attributes);
                }
            }
        }

        public virtual void SetAttributes(string path, bool isHidden, bool isReadOnly)
        {
            if (OperatingSystem.Id != OperatingSystemId.Windows)
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
        public virtual void SwapFiles(string file1, string file2)
        {
            if (string.IsNullOrEmpty(file1))
            {
                throw new ArgumentNullException(nameof(file1));
            }

            if (string.IsNullOrEmpty(file2))
            {
                throw new ArgumentNullException(nameof(file2));
            }

            var temp1 = Path.Combine(_tempPath, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

            // Copying over will fail against hidden files
            SetHidden(file1, false);
            SetHidden(file2, false);

            Directory.CreateDirectory(_tempPath);
            File.Copy(file1, temp1, true);

            File.Copy(file2, file1, true);
            File.Copy(temp1, file2, true);
        }

        public virtual bool ContainsSubPath(string parentPath, string path)
        {
            if (string.IsNullOrEmpty(parentPath))
            {
                throw new ArgumentNullException(nameof(parentPath));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var separatorChar = Path.DirectorySeparatorChar;

            return path.IndexOf(parentPath.TrimEnd(separatorChar) + separatorChar, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public virtual bool IsRootPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var parent = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(parent))
            {
                return false;
            }

            return true;
        }

        public virtual string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path.TrimEnd(Path.DirectorySeparatorChar);
        }

        public virtual bool AreEqual(string path1, string path2)
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

        public virtual string GetFileNameWithoutExtension(FileSystemMetadata info)
        {
            if (info.IsDirectory)
            {
                return info.Name;
            }

            return Path.GetFileNameWithoutExtension(info.FullName);
        }

        public virtual bool IsPathFile(string path)
        {
            // Cannot use Path.IsPathRooted because it returns false under mono when using windows-based paths, e.g. C:\\
            if (path.IndexOf("://", StringComparison.OrdinalIgnoreCase) != -1 &&
                !path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public virtual void DeleteFile(string path)
        {
            SetAttributes(path, false, false);
            File.Delete(path);
        }

        public virtual List<FileSystemMetadata> GetDrives()
        {
            // check for ready state to avoid waiting for drives to timeout
            // some drives on linux have no actual size or are used for other purposes
            return DriveInfo.GetDrives().Where(d => d.IsReady && d.TotalSize != 0 && d.DriveType != DriveType.Ram)
                .Select(d => new FileSystemMetadata
                {
                    Name = d.Name,
                    FullName = d.RootDirectory.FullName,
                    IsDirectory = true
                }).ToList();
        }

        public virtual IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return ToMetadata(new DirectoryInfo(path).EnumerateDirectories("*", searchOption));
        }

        public virtual IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false)
        {
            return GetFiles(path, null, false, recursive);
        }

        public virtual IEnumerable<FileSystemMetadata> GetFiles(string path, IReadOnlyList<string> extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // On linux and osx the search pattern is case sensitive
            // If we're OK with case-sensitivity, and we're only filtering for one extension, then use the native method
            if ((enableCaseSensitiveExtensions || _isEnvironmentCaseInsensitive) && extensions != null && extensions.Count == 1)
            {
                return ToMetadata(new DirectoryInfo(path).EnumerateFiles("*" + extensions[0], searchOption));
            }

            var files = new DirectoryInfo(path).EnumerateFiles("*", searchOption);

            if (extensions != null && extensions.Count > 0)
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

        public virtual IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false)
        {
            var directoryInfo = new DirectoryInfo(path);
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return ToMetadata(directoryInfo.EnumerateDirectories("*", searchOption))
                .Concat(ToMetadata(directoryInfo.EnumerateFiles("*", searchOption)));
        }

        private IEnumerable<FileSystemMetadata> ToMetadata(IEnumerable<FileSystemInfo> infos)
        {
            return infos.Select(GetFileSystemMetadata);
        }

        public virtual IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateDirectories(path, "*", searchOption);
        }

        public virtual IEnumerable<string> GetFilePaths(string path, bool recursive = false)
        {
            return GetFilePaths(path, null, false, recursive);
        }

        public virtual IEnumerable<string> GetFilePaths(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
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

        public virtual IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.EnumerateFileSystemEntries(path, "*", searchOption);
        }

        public virtual void SetExecutable(string path)
        {
            if (OperatingSystem.Id == OperatingSystemId.Darwin)
            {
                RunProcess("chmod", "+x \"" + path + "\"", Path.GetDirectoryName(path));
            }
        }

        private static void RunProcess(string path, string args, string workingDirectory)
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
