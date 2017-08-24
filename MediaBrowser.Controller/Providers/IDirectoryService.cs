using System.Collections.Generic;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        FileSystemMetadata[] GetFileSystemEntries(string path);
        List<FileSystemMetadata> GetFiles(string path);
        FileSystemMetadata GetFile(string path);
    }
}