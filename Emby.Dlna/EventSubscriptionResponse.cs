#nullable enable
#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna
{
    public class EventSubscriptionResponse
    {
        public EventSubscriptionResponse()
        {
            Headers = new Dictionary<string, string>();
            Content = string.Empty;
            ContentType = string.Empty;
        }

        public string Content { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; }
    }
}
