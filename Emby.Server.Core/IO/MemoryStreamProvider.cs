using System.IO;
using MediaBrowser.Model.IO;
using Microsoft.IO;

namespace Emby.Server.Core.IO
{
    public class RecyclableMemoryStreamProvider : IMemoryStreamFactory
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

        public bool TryGetBuffer(MemoryStream stream, out byte[] buffer)
        {
            buffer = stream.GetBuffer();
            return true;
        }
    }

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
