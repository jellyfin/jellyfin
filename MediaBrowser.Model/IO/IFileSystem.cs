#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Interface IFileSystem.
    /// </summary>
    public interface IFileSystem
    {
        void AddShortcutHandler(IShortcutHandler handler);

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        bool IsShortcut(string filename);

        /// <summary>
        /// Resolves the shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        string ResolveShortcut(string filename);

        /// <summary>
        /// Creates the shortcut.
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <param name="target">The target.</param>
        void CreateShortcut(string shortcutPath, string target);

        string MakeAbsolutePath(string folderPath, string filePath);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata" /> object for the specified file or directory path.
        /// </summary>
        /// <param name="path">A path to a file or directory.</param>
        /// <returns>A <see cref="FileSystemMetadata" /> object.</returns>
        /// <remarks>If the specified path points to a directory, the returned <see cref="FileSystemMetadata" /> object's
        /// <see cref="FileSystemMetadata.IsDirectory" /> property will be set to true and all other properties will reflect the properties of the directory.</remarks>
        FileSystemMetadata GetFileSystemInfo(string path);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata" /> object for the specified file path.
        /// </summary>
        /// <param name="path">A path to a file.</param>
        /// <returns>A <see cref="FileSystemMetadata" /> object.</returns>
        /// <remarks><para>If the specified path points to a directory, the returned <see cref="FileSystemMetadata" /> object's
        /// <see cref="FileSystemMetadata.IsDirectory" /> property and the <see cref="FileSystemMetadata.Exists" /> property will both be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="M:IFileSystem.GetFileSystemInfo(System.String)" />.</para></remarks>
        FileSystemMetadata GetFileInfo(string path);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata" /> object for the specified directory path.
        /// </summary>
        /// <param name="path">A path to a directory.</param>
        /// <returns>A <see cref="FileSystemMetadata" /> object.</returns>
        /// <remarks><para>If the specified path points to a file, the returned <see cref="FileSystemMetadata" /> object's
        /// <see cref="FileSystemMetadata.IsDirectory" /> property will be set to true and the <see cref="FileSystemMetadata.Exists" /> property will be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="M:IFileSystem.GetFileSystemInfo(System.String)" />.</para></remarks>
        FileSystemMetadata GetDirectoryInfo(string path);

        /// <summary>
        /// Gets the valid filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        string GetValidFilename(string filename);

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCreationTimeUtc(FileSystemMetadata info);

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCreationTimeUtc(string path);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(FileSystemMetadata info);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(string path);

        /// <summary>
        /// Swaps the files.
        /// </summary>
        /// <param name="file1">The file1.</param>
        /// <param name="file2">The file2.</param>
        void SwapFiles(string file1, string file2);

        bool AreEqual(string path1, string path2);

        /// <summary>
        /// Determines whether [contains sub path] [the specified parent path].
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [contains sub path] [the specified parent path]; otherwise, <c>false</c>.</returns>
        bool ContainsSubPath(string parentPath, string path);

        /// <summary>
        /// Determines whether [is root path] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is root path] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsRootPath(string path);

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string NormalizePath(string path);

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>System.String.</returns>
        string GetFileNameWithoutExtension(FileSystemMetadata info);

        /// <summary>
        /// Determines whether [is path file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is path file] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsPathFile(string path);

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        void DeleteFile(string path);

        /// <summary>
        /// Gets the directories.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;DirectoryInfo&gt;.</returns>
        IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false);

        /// <summary>
        /// Gets the files.
        /// </summary>
        IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false);

        IEnumerable<FileSystemMetadata> GetFiles(string path, IReadOnlyList<string> extensions, bool enableCaseSensitiveExtensions, bool recursive);

        /// <summary>
        /// Gets the file system entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;FileSystemMetadata&gt;.</returns>
        IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false);

        /// <summary>
        /// Gets the directory paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false);

        /// <summary>
        /// Gets the file paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetFilePaths(string path, bool recursive = false);

        IEnumerable<string> GetFilePaths(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive);

        /// <summary>
        /// Gets the file system entry paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false);

        void SetHidden(string path, bool isHidden);
        void SetReadOnly(string path, bool readOnly);
        void SetAttributes(string path, bool isHidden, bool readOnly);
        List<FileSystemMetadata> GetDrives();
        void SetExecutable(string path);
    }
}
