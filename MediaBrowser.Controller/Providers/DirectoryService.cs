using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        List<FileSystemInfo> GetFileSystemEntries(string path);
        IEnumerable<FileInfo> GetFiles(string path);
        IEnumerable<DirectoryInfo> GetDirectories(string path);
        FileInfo GetFile(string path);
        DirectoryInfo GetDirectory(string path);
    }

    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger _logger;

        private readonly Dictionary<string, List<FileSystemInfo>> _cache = new Dictionary<string, List<FileSystemInfo>>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(ILogger logger)
        {
            _logger = logger;
        }

        public List<FileSystemInfo> GetFileSystemEntries(string path)
        {
            List<FileSystemInfo> entries;

            if (!_cache.TryGetValue(path, out entries))
            {
                //_logger.Debug("Getting files for " + path);

                try
                {
                    entries = new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).ToList();
                }
                catch (DirectoryNotFoundException)
                {
                    entries = new List<FileSystemInfo>();
                }

                _cache.Add(path, entries);
            }

            return entries;
        }

        public IEnumerable<FileInfo> GetFiles(string path)
        {
            return GetFileSystemEntries(path).OfType<FileInfo>();
        }

        public IEnumerable<DirectoryInfo> GetDirectories(string path)
        {
            return GetFileSystemEntries(path).OfType<DirectoryInfo>();
        }

        public FileInfo GetFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var filename = Path.GetFileName(path);

            return GetFiles(directory).FirstOrDefault(i => string.Equals(i.Name, filename, StringComparison.OrdinalIgnoreCase));
        }


        public DirectoryInfo GetDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var name = Path.GetFileName(path);

            return GetDirectories(directory).FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
