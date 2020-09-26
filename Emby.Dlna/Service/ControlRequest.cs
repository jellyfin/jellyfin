#pragma warning disable CS1591

using System.IO;
using Microsoft.AspNetCore.Http;

namespace Emby.Dlna.Service
{
    public class ControlRequest
    {
        public ControlRequest(IHeaderDictionary headers, Stream inputXml, string targetServerUuid, string requestedUrl)
        {
            Headers = headers;
            RequestedUrl = requestedUrl;
            InputXml = inputXml;
            TargetServerUuId = targetServerUuid;
        }

        public IHeaderDictionary Headers { get; }

        public Stream InputXml { get; }

        public string TargetServerUuId { get; }

        public string RequestedUrl { get; }
    }
}
