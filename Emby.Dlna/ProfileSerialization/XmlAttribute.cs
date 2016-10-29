using System.Xml.Serialization;

namespace Emby.Dlna.ProfileSerialization
{
    public class XmlAttribute
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}