#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        // TODO replace with one shared bounded cache.
        private const int MaxCachedRecords = 100_000;
        private const int AccessIntervalMs = 1_000;
        // Timeout cache if no access for 5 minutes.
        private const int IdleTimeoutMs = 5 * 60 * 1_000;

        private readonly ConcurrentDictionary<string, FileSystemMetadata[]> _cache = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, FileSystemMetadata> _fileCache = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, List<string>> _filePathCache = new(StringComparer.Ordinal);

        private readonly IFileSystem _fileSystem;

        // ConcurrentDictionary.Count locks the dictionary, so keep an estimated counter.
        // Concurrent factory runs can overcount and a clear racing an add can undercount,
        // it only has to be roughly right.
        private int _recordCount;
        private long _lastAccess = Environment.TickCount64;

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            DropCacheIfIdleOrFull();

            return _cache.GetOrAdd(
                path,
                static (p, state) =>
                {
                    FileSystemMetadata[] entries;
                    try
                    {
                        entries = state.FileSystem.GetFileSystemEntries(p).ToArray();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        entries = [];
                    }

                    Interlocked.Add(ref state.Service._recordCount, entries.Length + 1);
                    return entries;
                },
                (FileSystem: _fileSystem, Service: this));
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
            DropCacheIfIdleOrFull();

            if (!_fileCache.TryGetValue(path, out var result))
            {
                var file = _fileSystem.GetFileSystemInfo(path);
                if (file?.Exists ?? false)
                {
                    result = file;
                    if (_fileCache.TryAdd(path, result))
                    {
                        Interlocked.Increment(ref _recordCount);
                    }
                }
            }

            return result;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache)
        {
            if (clearCache && _filePathCache.TryRemove(path, out var cached))
            {
                Interlocked.Add(ref _recordCount, -(cached.Count + 1));
            }

            DropCacheIfIdleOrFull();

            var filePaths = _filePathCache.GetOrAdd(
                path,
                static (p, state) =>
                {
                    List<string> filePaths;
                    try
                    {
                        filePaths = state.FileSystem.GetFilePaths(p).OrderBy(x => x).ToList();
                    }
                    catch (DirectoryNotFoundException)
                    {
                        filePaths = [];
                    }

                    Interlocked.Add(ref state.Service._recordCount, filePaths.Count + 1);
                    return filePaths;
                },
                (FileSystem: _fileSystem, Service: this));

            return filePaths;
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

        private void DropCacheIfIdleOrFull()
        {
            var nowMs = Environment.TickCount64;
            var idleMs = nowMs - Volatile.Read(ref _lastAccess);

            if (idleMs >= IdleTimeoutMs || Volatile.Read(ref _recordCount) >= MaxCachedRecords)
            {
                _cache.Clear();
                _fileCache.Clear();
                _filePathCache.Clear();
                Volatile.Write(ref _recordCount, 0);
                Volatile.Write(ref _lastAccess, nowMs);
                return;
            }

            if (idleMs >= AccessIntervalMs)
            {
                Volatile.Write(ref _lastAccess, nowMs);
            }
        }

        private void Forget(string path)
        {
            if (_cache.TryRemove(path, out var entries))
            {
                Interlocked.Add(ref _recordCount, -(entries.Length + 1));
            }

            if (_fileCache.TryRemove(path, out _))
            {
                Interlocked.Decrement(ref _recordCount);
            }

            if (_filePathCache.TryRemove(path, out var filePaths))
            {
                Interlocked.Add(ref _recordCount, -(filePaths.Count + 1));
            }
        }
    }
}
