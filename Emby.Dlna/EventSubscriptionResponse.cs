#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna
{
    public class EventSubscriptionResponse
    {
        public EventSubscriptionResponse(string content, string contentType)
        {
            Content = content;
            ContentType = contentType;
            Headers = new Dictionary<string, string>();
        }

        public string Content { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; }
    }
}
