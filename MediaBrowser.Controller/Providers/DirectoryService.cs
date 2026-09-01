#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitFaster.Caching.Lru;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private const int DirectoryCacheSize = 2048;
        private const int FileCacheSize = 4096;

        // A bounded LRU sizes its table up front, so it costs several kilobytes while still empty.
        // One instance is a DI singleton and lives for the process, but the library code also news
        // one up per item in several loops and hands it to QueueRefresh, which holds on to it until
        // the refresh runs. Those instances usually ask about a single path, so each cache waits
        // until something actually looks in it.
        private readonly Lazy<FastConcurrentLru<string, FileSystemMetadata[]>> _cache
            = new(static () => new(Environment.ProcessorCount, DirectoryCacheSize, StringComparer.Ordinal));

        private readonly Lazy<FastConcurrentLru<string, FileSystemMetadata>> _fileCache
            = new(static () => new(Environment.ProcessorCount, FileCacheSize, StringComparer.Ordinal));

        private readonly Lazy<FastConcurrentLru<string, List<string>>> _filePathCache
            = new(static () => new(Environment.ProcessorCount, DirectoryCacheSize, StringComparer.Ordinal));

        private readonly IFileSystem _fileSystem;

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return _cache.Value.GetOrAdd(
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
            if (!_fileCache.Value.TryGet(path, out var result))
            {
                var file = _fileSystem.GetFileSystemInfo(path);

                // Only a hit is remembered. A miss is the one answer that changes on its own, when
                // the file the path names turns up.
                if (file?.Exists ?? false)
                {
                    result = file;
                    _fileCache.Value.AddOrUpdate(path, result);
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
                _filePathCache.Value.TryRemove(path, out _);
            }

            var filePaths = _filePathCache.Value.GetOrAdd(
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

            return filePaths;
        }

        public bool IsAccessible(string path)
        {
            return _fileSystem.GetFileSystemEntryPaths(path).Any();
        }
    }
}
