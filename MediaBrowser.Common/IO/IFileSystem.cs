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
    }
}
