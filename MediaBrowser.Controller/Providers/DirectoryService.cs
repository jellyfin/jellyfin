using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        private readonly ConcurrentDictionary<string, FileSystemMetadata[]> _cache =
            new ConcurrentDictionary<string, FileSystemMetadata[]>(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, FileSystemMetadata> _fileCache =
        new ConcurrentDictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public DirectoryService(IFileSystem fileSystem)
            : this(new NullLogger(), fileSystem)
        {
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            return GetFileSystemEntries(path, false);
        }

        private FileSystemMetadata[] GetFileSystemEntries(string path, bool clearCache)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            FileSystemMetadata[] entries;

            if (clearCache)
            {
                FileSystemMetadata[] removed;

                _cache.TryRemove(path, out removed);
            }

            if (!_cache.TryGetValue(path, out entries))
            {
                //_logger.Debug("Getting files for " + path);

                try
                {
                    // using EnumerateFileSystemInfos doesn't handle reparse points (symlinks)
                    entries = _fileSystem.GetFileSystemEntries(path).ToArray();
                }
                catch (IOException)
                {
                    entries = new FileSystemMetadata[] { };
                }

                _cache.TryAdd(path, entries);
            }

            return entries;
        }

        public List<FileSystemMetadata> GetFiles(string path)
        {
            return GetFiles(path, false);
        }

        public List<FileSystemMetadata> GetFiles(string path, bool clearCache)
        {
            var list = new List<FileSystemMetadata>();
            var items = GetFileSystemEntries(path, clearCache);
            foreach (var item in items)
            {
                if (!item.IsDirectory)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public FileSystemMetadata GetFile(string path)
        {
            FileSystemMetadata file;
            if (!_fileCache.TryGetValue(path, out file))
            {
                file = _fileSystem.GetFileInfo(path);

                if (file != null && file.Exists)
                {
                    _fileCache.TryAdd(path, file);
                }
                else
                {
                    return null;
                }
            }

            return file;
            //return _fileSystem.GetFileInfo(path);
        }
    }
}
