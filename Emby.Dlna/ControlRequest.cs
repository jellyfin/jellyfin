#pragma warning disable CS1591

using System.IO;
using Microsoft.AspNetCore.Http;

namespace Emby.Dlna
{
    public class ControlRequest
    {
        public IHeaderDictionary Headers { get; set; }

        public Stream InputXml { get; set; }

        public string TargetServerUuId { get; set; }

        public string RequestedUrl { get; set; }

        public ControlRequest()
        {
            Headers = new HeaderDictionary();
        }
    }
}
