#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BitFaster.Caching.Lru;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private static readonly ConditionalWeakTable<IFileSystem, DirectoryCache> _caches = [];

        private readonly IFileSystem _fileSystem;
        private readonly DirectoryCache _cache;

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _cache = _caches.GetValue(fileSystem, static _ => new DirectoryCache());
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return _cache.Entries.GetOrAdd(
                path,
                static (p, fileSystem) =>
                {
                    try
                    {
                        return fileSystem.GetFileSystemEntries(p).ToArray();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return [];
                    }
                },
                _fileSystem);
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
            if (!_cache.Files.TryGet(path, out var result))
            {
                var file = _fileSystem.GetFileSystemInfo(path);

                // Only cache hits: a missing file can turn up later.
                if (file?.Exists ?? false)
                {
                    result = file;
                    _cache.Files.AddOrUpdate(path, result);
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
                // Not Invalidate(), which would also drop the parent listing for no reason here.
                Forget(path);
            }

            return _cache.FilePaths.GetOrAdd(
                path,
                static (p, fileSystem) =>
                {
                    try
                    {
                        return fileSystem.GetFilePaths(p).OrderBy(x => x).ToList();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return [];
                    }
                },
                _fileSystem);
        }

        public void Invalidate(string path)
        {
            Forget(path);

            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                Forget(parent);
            }
        }

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }

        private void Forget(string path)
        {
            _cache.Entries.TryRemove(path, out _);
            _cache.Files.TryRemove(path, out _);
            _cache.FilePaths.TryRemove(path, out _);
        }

        private sealed class DirectoryCache
        {
            private const int DirectoryCacheSize = 2048;
            private const int FileCacheSize = 8192;

            // The cache outlives the DirectoryService instances reading it, so entries need their
            // own staleness bound. A long refresh can outlive it and re-read a directory partway.
            private static readonly TimeSpan _entryLifetime = TimeSpan.FromMinutes(1);

            public ConcurrentTLru<string, FileSystemMetadata[]> Entries { get; }
                = new(Environment.ProcessorCount, DirectoryCacheSize, StringComparer.Ordinal, _entryLifetime);

            public ConcurrentTLru<string, FileSystemMetadata> Files { get; }
                = new(Environment.ProcessorCount, FileCacheSize, StringComparer.Ordinal, _entryLifetime);

            public ConcurrentTLru<string, List<string>> FilePaths { get; }
                = new(Environment.ProcessorCount, DirectoryCacheSize, StringComparer.Ordinal, _entryLifetime);
        }
    }
}
