#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private readonly IFileSystem _fileSystem;

        private readonly ConcurrentDictionary<string, FileSystemMetadata[]> _cache = new (StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, FileSystemMetadata> _fileCache = new (StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, List<string>> _filePathCache = new (StringComparer.Ordinal);

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return _cache.GetOrAdd(path, p => _fileSystem.GetFileSystemEntries(p).ToArray());
        }

        public List<FileSystemMetadata> GetFiles(string path)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path);
            foreach (var item in items)
            {
                if (!item.IsDirectory)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public FileSystemMetadata? GetFile(string path)
        {
            if (!_fileCache.TryGetValue(path, out var result))
            {
                var file = _fileSystem.GetFileInfo(path);
                var res = file != null && file.Exists ? file : null;
                if (res != null)
                {
                    result = res;
                    _fileCache.TryAdd(path, result);
                }
            }

            return result;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache)
        {
            if (clearCache)
            {
                _filePathCache.TryRemove(path, out _);
            }

            return _filePathCache.GetOrAdd(path, p => _fileSystem.GetFilePaths(p).ToList());
        }
    }
}
