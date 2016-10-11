using System.IO;

namespace MediaBrowser.Common.IO
{
    public interface IMemoryStreamProvider
    {
        MemoryStream CreateNew();
        MemoryStream CreateNew(int capacity);
        MemoryStream CreateNew(byte[] buffer);
    }
}
