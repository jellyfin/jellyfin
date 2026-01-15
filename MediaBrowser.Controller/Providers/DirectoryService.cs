#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using BitFaster.Caching.Lru;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private const int CacheSize = 10000;

        // Use FastConcurrentLru for bounded cache with automatic eviction (prevents unbounded memory growth)
        private static readonly FastConcurrentLru<string, FileSystemMetadata[]> _cache =
            new(Environment.ProcessorCount, CacheSize, StringComparer.Ordinal);

        private static readonly FastConcurrentLru<string, FileSystemMetadata> _fileCache =
            new(Environment.ProcessorCount, CacheSize, StringComparer.Ordinal);

        private static readonly FastConcurrentLru<string, List<string>> _filePathCache =
            new(Environment.ProcessorCount, CacheSize, StringComparer.Ordinal);

        private readonly IFileSystem _fileSystem;

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            if (_cache.TryGet(path, out var entries))
            {
                return entries;
            }

            entries = _fileSystem.GetFileSystemEntries(path).ToArray();
            _cache.AddOrUpdate(path, entries);
            return entries;
        }

        public List<FileSystemMetadata> GetDirectories(string path)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.IsDirectory)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public List<FileSystemMetadata> GetFiles(string path)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!item.IsDirectory)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public FileSystemMetadata? GetFile(string path)
        {
            var entry = GetFileSystemEntry(path);
            return entry is not null && !entry.IsDirectory ? entry : null;
        }

        public FileSystemMetadata? GetDirectory(string path)
        {
            var entry = GetFileSystemEntry(path);
            return entry is not null && entry.IsDirectory ? entry : null;
        }

        public FileSystemMetadata? GetFileSystemEntry(string path)
        {
            if (_fileCache.TryGet(path, out var result))
            {
                return result;
            }

            var file = _fileSystem.GetFileSystemInfo(path);
            if (file?.Exists ?? false)
            {
                _fileCache.AddOrUpdate(path, file);
                return file;
            }

            return null;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache, bool sort = false)
        {
            if (clearCache)
            {
                _filePathCache.TryRemove(path, out _);
            }

            if (!_filePathCache.TryGet(path, out var filePaths))
            {
                filePaths = _fileSystem.GetFilePaths(path).ToList();
                _filePathCache.AddOrUpdate(path, filePaths);
            }

            if (sort)
            {
                // Create a copy to avoid modifying cached list
                var sortedPaths = new List<string>(filePaths);
                sortedPaths.Sort();
                return sortedPaths;
            }

            return filePaths;
        }

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }
    }
}
