#nullable enable
#pragma warning disable CS1591

using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Emby.Dlna
{
    public class ControlRequest
    {
        public IHeaderDictionary Headers { get; }

        public Stream? InputXml { get; set; }

        public string TargetServerUuId { get; set; }

        public string RequestedUrl { get; set; }

        public ControlRequest(IHeaderDictionary headers, Stream? inputXML, string targetServerUuId, string requestedUrl)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            InputXml = inputXML ?? throw new ArgumentNullException(nameof(inputXML));
            TargetServerUuId = targetServerUuId ?? throw new ArgumentNullException(nameof(targetServerUuId));
            RequestedUrl = requestedUrl ?? throw new ArgumentNullException(nameof(headers));
        }
    }
}
