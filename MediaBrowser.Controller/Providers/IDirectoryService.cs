#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        FileSystemMetadata[] GetFileSystemEntries(string path);

        List<FileSystemMetadata> GetFiles(string path);

        FileSystemMetadata GetFile(string path);

        IReadOnlyList<string> GetFilePaths(string path);

        IReadOnlyList<string> GetFilePaths(string path, bool clearCache);
    }
}
