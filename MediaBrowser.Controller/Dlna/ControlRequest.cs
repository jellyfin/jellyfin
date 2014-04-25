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

    public class ControlResponse
    {
        public IDictionary<string, string> Headers { get; set; }

        public string Xml { get; set; }

        public bool IsSuccessful { get; set; }

        public ControlResponse()
        {
            Headers = new Dictionary<string, string>();
        }
    }
}
