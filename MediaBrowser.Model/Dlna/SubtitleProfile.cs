using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class SubtitleProfile
    {
        [XmlAttribute("format")]
        public string Format { get; set; }

        [XmlAttribute("method")]
        public SubtitleDeliveryMethod Method { get; set; }

        [XmlAttribute("didlMode")]
        public string DidlMode { get; set; }

    }
}