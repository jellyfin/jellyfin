using System.IO;
using MediaBrowser.Model.Services;

namespace Jellyfin.SocketSharp
{
    public class HttpFile : IHttpFile
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public Stream InputStream { get; set; }
    }
}
