using System.Collections.Generic;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path);
        IEnumerable<FileSystemMetadata> GetFiles(string path);
        IEnumerable<FileSystemMetadata> GetDirectories(string path);
        IEnumerable<FileSystemMetadata> GetFiles(string path, bool clearCache);
        FileSystemMetadata GetFile(string path);
        Dictionary<string, FileSystemMetadata> GetFileSystemDictionary(string path);
    }
}