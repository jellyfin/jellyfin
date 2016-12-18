using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Controller.Dlna
{
    public class ControlRequest
    {
        public IDictionary<string, string> Headers { get; set; }

        public Stream InputXml { get; set; }

        public string TargetServerUuId { get; set; }

        public string RequestedUrl { get; set; }

        public ControlRequest()
        {
            Headers = new Dictionary<string, string>();
        }
    }
}
