#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna.Service
{
    public class ControlResponse
    {
        public ControlResponse(string xml, bool isSuccessful)
        {
            Xml = xml;
            IsSuccessful = isSuccessful;
            Headers = new Dictionary<string, string>();
        }

        public Dictionary<string, string> Headers { get; }

        public string Xml { get; set; }

        public bool IsSuccessful { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Xml;
        }
    }
}
