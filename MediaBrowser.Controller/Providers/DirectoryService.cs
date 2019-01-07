using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        private readonly Dictionary<string, FileSystemMetadata[]> _cache = new Dictionary<string, FileSystemMetadata[]>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, FileSystemMetadata> _fileCache = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _filePathCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(ILogger logger, IFileSystem fileSystem)
        {
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            FileSystemMetadata[] entries;

            if (!_cache.TryGetValue(path, out entries))
            {
                //_logger.LogDebug("Getting files for " + path);

                entries = _fileSystem.GetFileSystemEntries(path).ToArray();

                //_cache.TryAdd(path, entries);
                _cache[path] = entries;
            }

            return entries;
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

        public FileSystemMetadata GetFile(string path)
        {
            FileSystemMetadata file;
            if (!_fileCache.TryGetValue(path, out file))
            {
                file = _fileSystem.GetFileInfo(path);

                if (file != null && file.Exists)
                {
                    //_fileCache.TryAdd(path, file);
                    _fileCache[path] = file;
                }
                else
                {
                    return null;
                }
            }

            return file;
            //return _fileSystem.GetFileInfo(path);
        }

        public List<string> GetFilePaths(string path)
        {
            return GetFilePaths(path, false);
        }

        public List<string> GetFilePaths(string path, bool clearCache)
        {
            List<string> result;
            if (clearCache || !_filePathCache.TryGetValue(path, out result))
            {
                result = _fileSystem.GetFilePaths(path).ToList();

                _filePathCache[path] = result;
            }

            return result;
        }

    }
}
