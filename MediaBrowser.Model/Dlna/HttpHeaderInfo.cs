using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class HttpHeaderInfo
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public HeaderMatchType Match { get; set; }
    }
}