#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna
{
    public class ControlResponse
    {
        public ControlResponse()
        {
            Headers = new Dictionary<string, string>();
            Xml = string.Empty;
        }

        public IDictionary<string, string> Headers { get; }

        public string Xml { get; set; }

        public bool IsSuccessful { get; set; }
    }
}
