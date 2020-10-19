#nullable disable
#pragma warning disable CS1591

using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class HttpHeaderInfo
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("match")]
        public HeaderMatchType Match { get; set; }
    }
}
