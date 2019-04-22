using System.Collections.Generic;
using Jellyfin.Model.IO;

namespace Jellyfin.Controller.Providers
{
    public interface IDirectoryService
    {
        FileSystemMetadata[] GetFileSystemEntries(string path);
        List<FileSystemMetadata> GetFiles(string path);
        FileSystemMetadata GetFile(string path);

        List<string> GetFilePaths(string path);
        List<string> GetFilePaths(string path, bool clearCache);
    }
}
