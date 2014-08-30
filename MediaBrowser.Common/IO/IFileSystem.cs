using System;
using System.IO;

namespace MediaBrowser.Common.IO
{
    /// <summary>
    /// Interface IFileSystem
    /// </summary>
    public interface IFileSystem
    {
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

        /// <summary>
        /// Gets the file system info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        FileSystemInfo GetFileSystemInfo(string path);

        /// <summary>
        /// Gets the valid filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        string GetValidFilename(string filename);

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCreationTimeUtc(FileSystemInfo info);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(FileSystemInfo info);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(string path);

        /// <summary>
        /// Gets the file stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <param name="isAsync">if set to <c>true</c> [is asynchronous].</param>
        /// <returns>FileStream.</returns>
        FileStream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share, bool isAsync = false);

        /// <summary>
        /// Swaps the files.
        /// </summary>
        /// <param name="file1">The file1.</param>
        /// <param name="file2">The file2.</param>
        void SwapFiles(string file1, string file2);

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
        /// Substitutes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns>System.String.</returns>
        string SubstitutePath(string path, string from, string to);

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>System.String.</returns>
        string GetFileNameWithoutExtension(FileSystemInfo info);

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string GetFileNameWithoutExtension(string path);
    }
}
