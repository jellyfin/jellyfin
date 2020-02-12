using System.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo
{
    class BdInfoFileInfo : BDInfo.IO.IFileInfo
    {
        FileSystemMetadata _impl = null;

        public string Name => _impl.Name;

        public string FullName => _impl.FullName;

        public string Extension => _impl.Extension;

        public long Length => _impl.Length;

        public bool IsDir => _impl.IsDirectory;

        public BdInfoFileInfo(FileSystemMetadata impl)
        {
            _impl = impl;
        }

        public System.IO.Stream OpenRead()
        {
            return new FileStream(FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }

        public System.IO.StreamReader OpenText()
        {
            return new System.IO.StreamReader(OpenRead());
        }
    }
}
