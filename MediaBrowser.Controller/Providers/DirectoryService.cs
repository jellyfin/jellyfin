#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using BitFaster.Caching.Lru;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        // Static cache shared across all DirectoryService instances to prevent unbounded memory growth
        // Using FastConcurrentLru for bounded, high-performance caching (as suggested in TODO)
        private static readonly FastConcurrentLru<string, object> _staticCache = new(10000); // Configurable capacity limit

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryService"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="memoryCache">Optional memory cache for filesystem metadata (deprecated - using static FastConcurrentLru instead).</param>
        [Obsolete("Memory cache parameter is deprecated. DirectoryService now uses a static FastConcurrentLru cache.")]
        public DirectoryService(IFileSystem fileSystem, object? memoryCache)
            : this(fileSystem)
        {
            // Parameter kept for backward compatibility but ignored
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            var cacheKey = $"fs_entries_{path}";
            if (_staticCache.TryGet(cacheKey, out var cached) && cached is FileSystemMetadata[] entries)
            {
                // Cache HIT - verify FastConcurrentLru is working
                BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache HIT for {CacheKey}", cacheKey);
                return entries;
            }

            // Cache MISS - will add to cache
            BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache MISS for {CacheKey}, adding to cache", cacheKey);
            entries = _fileSystem.GetFileSystemEntries(path).ToArray();
            _staticCache.AddOrUpdate(cacheKey, entries);
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
            if (_staticCache.TryGet(cacheKey, out var cached) && cached is FileSystemMetadata result)
            {
                BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache HIT for {CacheKey}", cacheKey);
                return result;
            }

            BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache MISS for {CacheKey}", cacheKey);
            var file = _fileSystem.GetFileSystemInfo(path);
            FileSystemMetadata? result2 = null;
            if (file?.Exists ?? false)
            {
                result2 = file;
                _staticCache.AddOrUpdate(cacheKey, result2);
            }

            return result2;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache, bool sort = false)
        {
            var cacheKey = $"fs_paths_{path}";
            if (clearCache)
            {
                _staticCache.TryRemove(cacheKey);
            }

            List<string>? filePaths = null;
            if (!_staticCache.TryGet(cacheKey, out var cached) || cached is not List<string> cachedPaths)
            {
                BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache MISS for {CacheKey}", cacheKey);
                filePaths = _fileSystem.GetFilePaths(path).ToList();
                _staticCache.AddOrUpdate(cacheKey, filePaths);
            }
            else
            {
                BaseItem.Logger?.LogInformation("[PR16038] DirectoryService cache HIT for {CacheKey}", cacheKey);
                filePaths = cachedPaths;
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
