using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        IEnumerable<FileSystemInfo> GetFileSystemEntries(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path);
        IEnumerable<FileSystemInfo> GetDirectories(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path, bool clearCache);
        FileSystemInfo GetFile(string path);
        Dictionary<string, FileSystemInfo> GetFileSystemDictionary(string path);
    }
}