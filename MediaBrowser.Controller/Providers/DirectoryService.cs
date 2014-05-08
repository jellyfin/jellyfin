using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Providers
{
    public interface IDirectoryService
    {
        List<FileSystemInfo> GetFileSystemEntries(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path, bool clearCache);
        FileSystemInfo GetFile(string path);
    }

    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, List<FileSystemInfo>> _cache = new ConcurrentDictionary<string, List<FileSystemInfo>>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(ILogger logger)
        {
            _logger = logger;
        }

        public List<FileSystemInfo> GetFileSystemEntries(string path)
        {
            return GetFileSystemEntries(path, false);
        }

        private List<FileSystemInfo> GetFileSystemEntries(string path, bool clearCache)
        {
            List<FileSystemInfo> entries;

            if (clearCache)
            {
                List<FileSystemInfo> removed;

                _cache.TryRemove(path, out removed);
            }

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

                _cache.TryAdd(path, entries);
            }

            return entries;
        }

        public IEnumerable<FileSystemInfo> GetFiles(string path)
        {
            return GetFiles(path, false);
        }

        public IEnumerable<FileSystemInfo> GetFiles(string path, bool clearCache)
        {
            return GetFileSystemEntries(path, clearCache).Where(i => (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory);
        }

        public FileSystemInfo GetFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var filename = Path.GetFileName(path);

            return GetFiles(directory).FirstOrDefault(i => string.Equals(i.Name, filename, StringComparison.OrdinalIgnoreCase));
        }
    }
}
