using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;

namespace Emby.Dlna.ProfileSerialization
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