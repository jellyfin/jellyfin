using System.Xml.Serialization;

namespace Jellyfin.Model.Dlna
{
    public class XmlAttribute
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
