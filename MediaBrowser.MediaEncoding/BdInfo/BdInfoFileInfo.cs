using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo
{
    class BdInfoFileInfo : BDInfo.IO.IFileInfo
    {
        IFileSystem _fileSystem = null;

        FileSystemMetadata _impl = null;

        public string Name => _impl.Name;

        public string FullName => _impl.FullName;

        public string Extension => _impl.Extension;

        public long Length => _impl.Length;

        public bool IsDir => _impl.IsDirectory;

        public BdInfoFileInfo(IFileSystem fileSystem, FileSystemMetadata impl)
        {
            _fileSystem = fileSystem;
            _impl = impl;
        }

        public System.IO.Stream OpenRead()
        {
            return _fileSystem.GetFileStream(FullName,
                FileOpenMode.Open,
                FileAccessMode.Read,
                FileShareMode.Read);
        }

        public System.IO.StreamReader OpenText()
        {
            return new System.IO.StreamReader(OpenRead());
        }
    }
}
