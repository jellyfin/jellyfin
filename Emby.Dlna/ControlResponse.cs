#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;

namespace Emby.Dlna
{
    public class ControlResponse
    {
        public ControlResponse()
        {
            Headers = new Dictionary<string, string>();
        }

        public IDictionary<string, string> Headers { get; set; }

        public string Xml { get; set; }

        public bool IsSuccessful { get; set; }
    }
}
