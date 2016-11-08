using System.IO;

namespace MediaBrowser.Model.IO
{
    public interface IMemoryStreamFactory
    {
        MemoryStream CreateNew();
        MemoryStream CreateNew(int capacity);
        MemoryStream CreateNew(byte[] buffer);
        bool TryGetBuffer(MemoryStream stream, out byte[] buffer);
    }
}
