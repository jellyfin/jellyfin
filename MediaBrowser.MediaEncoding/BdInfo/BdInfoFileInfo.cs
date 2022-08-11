#pragma warning disable CS1591

using System.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo
{
    public class BdInfoFileInfo : BDInfo.IO.IFileInfo
    {
        private FileSystemMetadata _impl;

        public BdInfoFileInfo(FileSystemMetadata impl)
        {
            _impl = impl;
        }

        public string Name => _impl.Name;

        public string FullName => _impl.FullName;

        public string Extension => _impl.Extension;

        public long Length => _impl.Length;

        public bool IsDir => _impl.IsDirectory;

        public Stream OpenRead()
        {
            return new FileStream(
                FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }

        public StreamReader OpenText()
        {
            return new StreamReader(OpenRead());
        }
    }
}
