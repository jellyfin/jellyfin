#pragma warning disable CA1002, CA1819, CS1591

using System.Collections.Generic;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        FileSystemMetadata[] GetFileSystemEntries(string path);

        List<FileSystemMetadata> GetDirectories(string path);

        List<FileSystemMetadata> GetFiles(string path);

        FileSystemMetadata? GetFile(string path);

        FileSystemMetadata? GetDirectory(string path);

        FileSystemMetadata? GetFileSystemEntry(string path);

        IReadOnlyList<string> GetFilePaths(string path);

        IReadOnlyList<string> GetFilePaths(string path, bool clearCache);

        /// <summary>
        /// Forgets what is cached about a path and about the directory containing it.
        /// </summary>
        /// <param name="path">The file or directory path that changed.</param>
        void Invalidate(string path);

        /// <summary>
        /// Moves a directory and forgets what is cached about both paths.
        /// </summary>
        /// <param name="source">The directory to move.</param>
        /// <param name="destination">The path to move the directory to.</param>
        void Move(string source, string destination);

        bool IsAccessible(string path);
    }
}
