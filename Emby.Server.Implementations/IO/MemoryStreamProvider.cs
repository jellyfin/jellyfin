using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    public class MemoryStreamProvider : IMemoryStreamFactory
    {
        public MemoryStream CreateNew()
        {
            return new MemoryStream();
        }

        public MemoryStream CreateNew(int capacity)
        {
            return new MemoryStream(capacity);
        }

        public MemoryStream CreateNew(byte[] buffer)
        {
            return new MemoryStream(buffer);
        }

        public bool TryGetBuffer(MemoryStream stream, out byte[] buffer)
        {
            buffer = stream.GetBuffer();
            return true;
        }
    }
}
