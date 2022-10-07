using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// Class ManagedFileSystem.
    /// </summary>
    public class ManagedFileSystem : IFileSystem
    {
        private readonly ILogger<ManagedFileSystem> _logger;

        private readonly List<IShortcutHandler> _shortcutHandlers = new List<IShortcutHandler>();
        private readonly string _tempPath;
        private static readonly bool _isEnvironmentCaseInsensitive = OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedFileSystem"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance to use.</param>
        /// <param name="applicationPaths">The <see cref="IApplicationPaths"/> instance to use.</param>
        public ManagedFileSystem(
            ILogger<ManagedFileSystem> logger,
            IApplicationPaths applicationPaths)
        {
            _logger = logger;
            _tempPath = applicationPaths.TempDirectory;
        }

        /// <inheritdoc />
        public virtual void AddShortcutHandler(IShortcutHandler handler)
        {
            _shortcutHandlers.Add(handler);
        }

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <c>null</c>.</exception>
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
        /// <exception cref="ArgumentNullException"><paramref name="filename"/> is <c>null</c>.</exception>
        public virtual string? ResolveShortcut(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var extension = Path.GetExtension(filename);
            var handler = _shortcutHandlers.Find(i => string.Equals(extension, i.Extension, StringComparison.OrdinalIgnoreCase));

            return handler?.Resolve(filename);
        }

        /// <inheritdoc />
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

                // if (!result.IsDirectory)
                // {
                //    result.IsHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                // }

                if (info is FileInfo fileInfo)
                {
                    result.Length = fileInfo.Length;

                    // Issue #2354 get the size of files behind symbolic links. Also Enum.HasFlag is bad as it boxes!
                    if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        try
                        {
                            using (var fileHandle = File.OpenHandle(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                result.Length = RandomAccess.GetLength(fileHandle);
                            }
                        }
                        catch (FileNotFoundException ex)
                        {
                            // Dangling symlinks cannot be detected before opening the file unfortunately...
                            _logger.LogError(ex, "Reading the file size of the symlink at {Path} failed. Marking the file as not existing.", fileInfo.FullName);
                            result.Exists = false;
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            _logger.LogError(ex, "Reading the file at {Path} failed due to a permissions exception.", fileInfo.FullName);
                        }
                    }
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
        public string GetValidFilename(string filename)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var first = filename.IndexOfAny(invalid);
            if (first == -1)
            {
                // Fast path for clean strings
                return filename;
            }

            return string.Create(
                filename.Length,
                (filename, invalid, first),
                (chars, state) =>
                {
                    state.filename.AsSpan().CopyTo(chars);

                    chars[state.first++] = ' ';

                    var len = chars.Length;
                    foreach (var c in state.invalid)
                    {
                        for (int i = state.first; i < len; i++)
                        {
                            if (chars[i] == c)
                            {
                                chars[i] = ' ';
                            }
                        }
                    }
                });
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
                _logger.LogError(ex, "Error determining CreationTimeUtc for {FullName}", info.FullName);
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

        /// <inheritdoc />
        public virtual DateTime GetCreationTimeUtc(FileSystemMetadata info)
        {
            return info.CreationTimeUtc;
        }

        /// <inheritdoc />
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
                _logger.LogError(ex, "Error determining LastAccessTimeUtc for {FullName}", info.FullName);
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

        /// <inheritdoc />
        public virtual void SetHidden(string path, bool isHidden)
        {
            if (!OperatingSystem.IsWindows())
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

        /// <inheritdoc />
        public virtual void SetAttributes(string path, bool isHidden, bool readOnly)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            var info = GetExtendedFileSystemInfo(path);

            if (!info.Exists)
            {
                return;
            }

            if (info.IsReadOnly == readOnly && info.IsHidden == isHidden)
            {
                return;
            }

            var attributes = File.GetAttributes(path);

            if (readOnly)
            {
                attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
            }

            if (isHidden)
            {
                attributes |= FileAttributes.Hidden;
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

        /// <inheritdoc />
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

            return path.Contains(
                Path.TrimEndingDirectorySeparator(parentPath) + Path.DirectorySeparatorChar,
                _isEnvironmentCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <inheritdoc />
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

            return Path.TrimEndingDirectorySeparator(path);
        }

        /// <inheritdoc />
        public virtual bool AreEqual(string path1, string path2)
        {
            return string.Equals(
                NormalizePath(path1),
                NormalizePath(path2),
                _isEnvironmentCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public virtual string GetFileNameWithoutExtension(FileSystemMetadata info)
        {
            if (info.IsDirectory)
            {
                return info.Name;
            }

            return Path.GetFileNameWithoutExtension(info.FullName);
        }

        /// <inheritdoc />
        public virtual bool IsPathFile(string path)
        {
            if (path.Contains("://", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public virtual void DeleteFile(string path)
        {
            SetAttributes(path, false, false);
            File.Delete(path);
        }

        /// <inheritdoc />
        public virtual IEnumerable<FileSystemMetadata> GetDrives()
        {
            // check for ready state to avoid waiting for drives to timeout
            // some drives on linux have no actual size or are used for other purposes
            return DriveInfo.GetDrives()
                .Where(
                    d => (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Network || d.DriveType == DriveType.Removable)
                        && d.IsReady
                        && d.TotalSize != 0)
                .Select(d => new FileSystemMetadata
                {
                    Name = d.Name,
                    FullName = d.RootDirectory.FullName,
                    IsDirectory = true
                });
        }

        /// <inheritdoc />
        public virtual IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false)
        {
            return ToMetadata(new DirectoryInfo(path).EnumerateDirectories("*", GetEnumerationOptions(recursive)));
        }

        /// <inheritdoc />
        public virtual IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false)
        {
            return GetFiles(path, null, false, recursive);
        }

        /// <inheritdoc />
        public virtual IEnumerable<FileSystemMetadata> GetFiles(string path, IReadOnlyList<string>? extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var enumerationOptions = GetEnumerationOptions(recursive);

            // On linux and osx the search pattern is case sensitive
            // If we're OK with case-sensitivity, and we're only filtering for one extension, then use the native method
            if ((enableCaseSensitiveExtensions || _isEnvironmentCaseInsensitive) && extensions != null && extensions.Count == 1)
            {
                return ToMetadata(new DirectoryInfo(path).EnumerateFiles("*" + extensions[0], enumerationOptions));
            }

            var files = new DirectoryInfo(path).EnumerateFiles("*", enumerationOptions);

            if (extensions != null && extensions.Count > 0)
            {
                files = files.Where(i =>
                {
                    var ext = i.Extension.AsSpan();
                    if (ext.IsEmpty)
                    {
                        return false;
                    }

                    return extensions.Contains(ext, StringComparison.OrdinalIgnoreCase);
                });
            }

            return ToMetadata(files);
        }

        /// <inheritdoc />
        public virtual IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false)
        {
            var directoryInfo = new DirectoryInfo(path);
            var enumerationOptions = GetEnumerationOptions(recursive);

            return ToMetadata(directoryInfo.EnumerateFileSystemInfos("*", enumerationOptions));
        }

        private IEnumerable<FileSystemMetadata> ToMetadata(IEnumerable<FileSystemInfo> infos)
        {
            return infos.Select(GetFileSystemMetadata);
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false)
        {
            return Directory.EnumerateDirectories(path, "*", GetEnumerationOptions(recursive));
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> GetFilePaths(string path, bool recursive = false)
        {
            return GetFilePaths(path, null, false, recursive);
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> GetFilePaths(string path, string[]? extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var enumerationOptions = GetEnumerationOptions(recursive);

            // On linux and osx the search pattern is case sensitive
            // If we're OK with case-sensitivity, and we're only filtering for one extension, then use the native method
            if ((enableCaseSensitiveExtensions || _isEnvironmentCaseInsensitive) && extensions != null && extensions.Length == 1)
            {
                return Directory.EnumerateFiles(path, "*" + extensions[0], enumerationOptions);
            }

            var files = Directory.EnumerateFiles(path, "*", enumerationOptions);

            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(i =>
                {
                    var ext = Path.GetExtension(i.AsSpan());
                    if (ext.IsEmpty)
                    {
                        return false;
                    }

                    return extensions.Contains(ext, StringComparison.OrdinalIgnoreCase);
                });
            }

            return files;
        }

        /// <inheritdoc />
        public virtual IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false)
        {
            return Directory.EnumerateFileSystemEntries(path, "*", GetEnumerationOptions(recursive));
        }

        /// <inheritdoc />
        public virtual bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc />
        public virtual bool FileExists(string path)
        {
            return File.Exists(path);
        }

        private EnumerationOptions GetEnumerationOptions(bool recursive)
        {
            return new EnumerationOptions
            {
                RecurseSubdirectories = recursive,
                IgnoreInaccessible = true,
                // Don't skip any files.
                AttributesToSkip = 0
            };
        }
    }
}
