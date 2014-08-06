using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class SubtitleProfile
    {
        [XmlAttribute("format")]
        public string Format { get; set; }

        [XmlAttribute("protocol")]
        public string Protocol { get; set; }

        [XmlAttribute("method")]
        public SubtitleDeliveryMethod Method { get; set; }
    }
}