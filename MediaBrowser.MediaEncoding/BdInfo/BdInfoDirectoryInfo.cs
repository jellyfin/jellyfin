using System;
using System.Linq;
using BDInfo.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo
{
    class BdInfoDirectoryInfo : IDirectoryInfo
    {
        private readonly IFileSystem _fileSystem = null;

        private readonly FileSystemMetadata _impl = null;

        public BdInfoDirectoryInfo(IFileSystem fileSystem, string path)
        {
            _fileSystem = fileSystem;
            _impl = _fileSystem.GetDirectoryInfo(path);
        }

        private BdInfoDirectoryInfo(IFileSystem fileSystem, FileSystemMetadata impl)
        {
            _fileSystem = fileSystem;
            _impl = impl;
        }

        public string Name => _impl.Name;

        public string FullName => _impl.FullName;

        public IDirectoryInfo Parent
        {
            get
            {
                var parentFolder = System.IO.Path.GetDirectoryName(_impl.FullName);
                if (parentFolder != null)
                {
                    return new BdInfoDirectoryInfo(_fileSystem, parentFolder);
                }

                return null;
            }
        }

        public IDirectoryInfo[] GetDirectories()
        {
            return Array.ConvertAll(_fileSystem.GetDirectories(_impl.FullName).ToArray(),
                x => new BdInfoDirectoryInfo(_fileSystem, x));
        }

        public IFileInfo[] GetFiles()
        {
            return Array.ConvertAll(_fileSystem.GetFiles(_impl.FullName).ToArray(),
                x => new BdInfoFileInfo(x));
        }

        public IFileInfo[] GetFiles(string searchPattern)
        {
            return Array.ConvertAll(_fileSystem.GetFiles(_impl.FullName, new[] { searchPattern }, false, false).ToArray(),
                x => new BdInfoFileInfo(x));
        }

        public IFileInfo[] GetFiles(string searchPattern, System.IO.SearchOption searchOption)
        {
            return Array.ConvertAll(_fileSystem.GetFiles(_impl.FullName, new[] { searchPattern }, false,
                    searchOption.HasFlag(System.IO.SearchOption.AllDirectories)).ToArray(),
                x => new BdInfoFileInfo(x));
        }

        public static IDirectoryInfo FromFileSystemPath(Model.IO.IFileSystem fs, string path)
        {
            return new BdInfoDirectoryInfo(fs, path);
        }
    }
}
