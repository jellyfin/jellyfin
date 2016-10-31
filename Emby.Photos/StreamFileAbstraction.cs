using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using File = TagLib.File;

namespace Emby.Photos
{
    public class StreamFileAbstraction : File.IFileAbstraction
    {
        public StreamFileAbstraction(string name, Stream readStream)
        {
            // TODO: Fix deadlock when setting an actual writable Stream
            WriteStream = readStream;
            ReadStream = readStream;
            Name = name;
        }

        public string Name { get; private set; }

        public Stream ReadStream { get; private set; }

        public Stream WriteStream { get; private set; }

        public void CloseStream(Stream stream)
        {
            stream.Dispose();
        }
    }
}
