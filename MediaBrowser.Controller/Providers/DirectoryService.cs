#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Caching.Memory;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private static readonly MemoryCacheEntryOptions _defaultCacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1
        };

        private readonly IMemoryCache? _cache;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        public DirectoryService(IFileSystem fileSystem)
            : this(fileSystem, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="memoryCache">Optional memory cache for filesystem metadata.</param>
        public DirectoryService(IFileSystem fileSystem, IMemoryCache? memoryCache)
        {
            _fileSystem = fileSystem;
            _cache = memoryCache;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            var cacheKey = $"fs_entries_{path}";
            if (_cache?.TryGetValue<FileSystemMetadata[]>(cacheKey, out var cached) == true)
            {
                return cached!;
            }

            var entries = _fileSystem.GetFileSystemEntries(path).ToArray();
            _cache?.Set(cacheKey, entries, _defaultCacheOptions);
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
            var cacheKey = $"fs_entry_{path}";
            if (_cache?.TryGetValue<FileSystemMetadata>(cacheKey, out var cached) == true)
            {
                return cached;
            }

            var file = _fileSystem.GetFileSystemInfo(path);
            FileSystemMetadata? result = null;
            if (file?.Exists ?? false)
            {
                result = file;
                _cache?.Set(cacheKey, result, _defaultCacheOptions);
            }

            return result;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache, bool sort = false)
        {
            var cacheKey = $"fs_paths_{path}";
            if (clearCache)
            {
                _cache?.Remove(cacheKey);
            }

            List<string>? filePaths = null;
            if (_cache?.TryGetValue<List<string>>(cacheKey, out filePaths) != true)
            {
                filePaths = _fileSystem.GetFilePaths(path).ToList();
                _cache?.Set(cacheKey, filePaths, _defaultCacheOptions);
            }

            if (sort && filePaths is not null)
            {
                filePaths.Sort();
            }

            return filePaths ?? new List<string>();
        }

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }
    }
}
