using System.IO;
using MediaBrowser.Common.IO;
using Microsoft.IO;

namespace MediaBrowser.Common.Implementations.IO
{
    public class RecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

        public MemoryStream CreateNew()
        {
            return _manager.GetStream();
        }

        public MemoryStream CreateNew(int capacity)
        {
            return _manager.GetStream("RecyclableMemoryStream", capacity);
        }

        public MemoryStream CreateNew(byte[] buffer)
        {
            return _manager.GetStream("RecyclableMemoryStream", buffer, 0, buffer.Length);
        }
    }

    public class MemoryStreamProvider : IMemoryStreamProvider
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
    }
}
