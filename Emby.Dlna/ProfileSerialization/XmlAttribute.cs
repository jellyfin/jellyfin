using System.Xml.Serialization;

namespace MediaBrowser.Dlna.ProfileSerialization
{
    public class XmlAttribute
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}