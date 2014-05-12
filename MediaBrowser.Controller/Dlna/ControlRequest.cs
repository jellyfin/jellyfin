using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public class ControlRequest
    {
        public IDictionary<string, string> Headers { get; set; }

        public string InputXml { get; set; }

        public string TargetServerUuId { get; set; }

        public string RequestedUrl { get; set; }

        public ControlRequest()
        {
            Headers = new Dictionary<string, string>();
        }
    }
}
