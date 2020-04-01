using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Providers
{
    public class DirectoryService : IDirectoryService
    {
        private readonly IFileSystem _fileSystem;

        private readonly Dictionary<string, FileSystemMetadata[]> _cache = new Dictionary<string, FileSystemMetadata[]>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, FileSystemMetadata> _fileCache = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _filePathCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public DirectoryService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public FileSystemMetadata[] GetFileSystemEntries(string path)
        {
            if (!_cache.TryGetValue(path, out FileSystemMetadata[] entries))
            {
                entries = _fileSystem.GetFileSystemEntries(path).ToArray();

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
            if (!_fileCache.TryGetValue(path, out FileSystemMetadata file))
            {
                file = _fileSystem.GetFileInfo(path);

                if (file != null && file.Exists)
                {
                    _fileCache[path] = file;
                }
                else
                {
                    return null;
                }
            }

            return file;
        }

        public IReadOnlyList<string> GetFilePaths(string path)
            => GetFilePaths(path, false);

        public IReadOnlyList<string> GetFilePaths(string path, bool clearCache)
        {
            if (clearCache || !_filePathCache.TryGetValue(path, out List<string> result))
            {
                result = _fileSystem.GetFilePaths(path).ToList();

                _filePathCache[path] = result;
            }

            return result;
        }
    }
}
