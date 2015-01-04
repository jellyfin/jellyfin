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
        IEnumerable<FileSystemInfo> GetFileSystemEntries(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path);
        IEnumerable<FileSystemInfo> GetDirectories(string path);
        IEnumerable<FileSystemInfo> GetFiles(string path, bool clearCache);
        FileSystemInfo GetFile(string path);
        Dictionary<string, FileSystemInfo> GetFileSystemDictionary(string path);
    }

    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, Dictionary<string,FileSystemInfo>> _cache =
            new ConcurrentDictionary<string, Dictionary<string, FileSystemInfo>>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(ILogger logger)
        {
            _logger = logger;
        }

        public DirectoryService()
            : this(new NullLogger())
        {
        }

        public IEnumerable<FileSystemInfo> GetFileSystemEntries(string path)
        {
            return GetFileSystemEntries(path, false);
        }

        public Dictionary<string, FileSystemInfo> GetFileSystemDictionary(string path)
        {
            return GetFileSystemDictionary(path, false);
        }

        private Dictionary<string, FileSystemInfo> GetFileSystemDictionary(string path, bool clearCache)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            Dictionary<string, FileSystemInfo> entries;

            if (clearCache)
            {
                Dictionary<string, FileSystemInfo> removed;

                _cache.TryRemove(path, out removed);
            }

            if (!_cache.TryGetValue(path, out entries))
            {
                //_logger.Debug("Getting files for " + path);

                entries = new Dictionary<string, FileSystemInfo>(StringComparer.OrdinalIgnoreCase);
                
                try
                {
                    var list = new DirectoryInfo(path)
                        .EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);

                    // Seeing dupes on some users file system for some reason
                    foreach (var item in list)
                    {
                        entries[item.FullName] = item;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                }

                //var group = entries.ToLookup(i => Path.GetDirectoryName(i.FullName)).ToList();

                _cache.TryAdd(path, entries);
            }

            return entries;
        }

        private IEnumerable<FileSystemInfo> GetFileSystemEntries(string path, bool clearCache)
        {
            return GetFileSystemDictionary(path, clearCache).Values;
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

            var dict = GetFileSystemDictionary(directory, false);

            FileSystemInfo entry;
            dict.TryGetValue(path, out entry);

            return entry;
        }

        public IEnumerable<FileSystemInfo> GetDirectories(string path)
        {
            return GetFileSystemEntries(path, false).Where(i => (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory);
        }
    }
}
