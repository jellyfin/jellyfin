using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using Microsoft.IO;

namespace MediaBrowser.Common.Implementations.IO
{
    public class MemoryStreamProvider : IMemoryStreamProvider
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
}
