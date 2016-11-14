using System;
using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.IO
{
    public class MemoryStreamFactory : IMemoryStreamFactory
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
            ArraySegment<byte> arrayBuffer;
            stream.TryGetBuffer(out arrayBuffer);

            buffer = arrayBuffer.Array;
            return true;
        }
    }
}
