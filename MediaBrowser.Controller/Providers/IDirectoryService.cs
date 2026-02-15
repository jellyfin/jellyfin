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

        IReadOnlyList<string> GetFilePaths(string path, bool clearCache, bool sort = false);

        bool IsAccessible(string path);
    }
}
