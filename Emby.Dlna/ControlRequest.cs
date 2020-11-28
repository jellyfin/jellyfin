#pragma warning disable CS1591

using System.IO;
using Microsoft.AspNetCore.Http;

namespace Emby.Dlna
{
    public class ControlRequest
    {
        public ControlRequest(IHeaderDictionary headers, Stream inputXml, string targetServerUuId, string requestedUrl)
        {
            Headers = headers;
            InputXml = inputXml;
            TargetServerUuId = targetServerUuId;
            RequestedUrl = requestedUrl;
        }

        public IHeaderDictionary Headers { get; }

        public Stream InputXml { get; set; }

        public string TargetServerUuId { get; set; }

        public string RequestedUrl { get; set; }
    }
}
