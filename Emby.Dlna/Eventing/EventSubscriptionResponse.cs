#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna.Eventing
{
    public class EventSubscriptionResponse
    {
        public EventSubscriptionResponse()
        {
            Headers = new Dictionary<string, string>();
        }

        public string Content { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; }
    }
}
