#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BitFaster.Caching;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryService"/> class holding a cache of
        /// its own, so a test can pick a lifetime it is willing to wait out.
        /// </summary>
        /// <param name="fileSystem">The file system to read through.</param>
        /// <param name="entryLifetime">How long an entry is trusted after it was last read.</param>
        internal DirectoryService(IFileSystem fileSystem, TimeSpan entryLifetime)
        {
            _fileSystem = fileSystem;
            _cache = new DirectoryCache(entryLifetime);
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
            // Deliberately not a GetOrAdd: a path that does not exist is not remembered, so a file
            // appearing later is picked up without waiting for anything to invalidate it.
            if (!_cache.Files.TryGet(path, out var result))
            {
                var file = _fileSystem.GetFileSystemInfo(path);
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
                _cache.FilePaths.TryRemove(path);
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

        public void Move(string source, string destination)
        {
            Directory.Move(source, destination);

            Invalidate(source);
            Invalidate(destination);
        }

        public void TrimExpired()
            => _cache.TrimExpired();

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }

        private void Forget(string path)
        {
            _cache.Entries.TryRemove(path);
            _cache.Files.TryRemove(path);
            _cache.FilePaths.TryRemove(path);
        }

        private sealed class DirectoryCache
        {
            // Entries are whole directory listings, so a modest count still holds a lot of metadata.
            // Files are single entries, so that cache is allowed to be deeper.
            private const int DirectoryCacheSize = 2048;
            private const int FileCacheSize = 8192;

            // A DirectoryService no longer bounds how long its answers are trusted by dying, so a
            // lifetime does. This is a staleness bound, not a snapshot: a long refresh can outlive it
            // and re-read a directory partway through.
            private static readonly TimeSpan _defaultEntryLifetime = TimeSpan.FromMinutes(1);

            public DirectoryCache()
                : this(_defaultEntryLifetime)
            {
            }

            public DirectoryCache(TimeSpan entryLifetime)
            {
                // Reading a directory is expensive enough to be worth never doing twice at once:
                // siblings are resolved in parallel and share a containing folder, so a plain
                // GetOrAdd would run the same listing on every one of them and keep one result.
                Entries = new ConcurrentLruBuilder<string, FileSystemMetadata[]>()
                    .WithKeyComparer(StringComparer.Ordinal)
                    .WithCapacity(DirectoryCacheSize)
                    .WithExpireAfterAccess(entryLifetime)
                    .WithAtomicGetOrAdd()
                    .Build();

                Files = new ConcurrentLruBuilder<string, FileSystemMetadata>()
                    .WithKeyComparer(StringComparer.Ordinal)
                    .WithCapacity(FileCacheSize)
                    .WithExpireAfterAccess(entryLifetime)
                    .Build();

                FilePaths = new ConcurrentLruBuilder<string, List<string>>()
                    .WithKeyComparer(StringComparer.Ordinal)
                    .WithCapacity(DirectoryCacheSize)
                    .WithExpireAfterAccess(entryLifetime)
                    .WithAtomicGetOrAdd()
                    .Build();
            }

            public ICache<string, FileSystemMetadata[]> Entries { get; }

            public ICache<string, FileSystemMetadata> Files { get; }

            public ICache<string, List<string>> FilePaths { get; }

            public void TrimExpired()
            {
                TrimExpired(Entries);
                TrimExpired(Files);
                TrimExpired(FilePaths);
            }

            private static void TrimExpired<TValue>(ICache<string, TValue> cache)
            {
                // Nothing sweeps these caches on its own: a read only discards the key it touched and
                // an add only inspects the head of the queues, so an idle server holds on to whatever
                // the last scan left behind until something asks for it.
                var expiry = cache.Policy.ExpireAfterAccess;
                if (expiry.HasValue && expiry.Value is { } policy)
                {
                    policy.TrimExpired();
                }
            }
        }
    }
}
