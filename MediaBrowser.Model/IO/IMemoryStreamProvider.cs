using System.IO;

namespace MediaBrowser.Model.IO
{
    public interface IMemoryStreamProvider
    {
        MemoryStream CreateNew();
        MemoryStream CreateNew(int capacity);
        MemoryStream CreateNew(byte[] buffer);
    }
}
