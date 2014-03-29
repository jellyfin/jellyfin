using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Controller.Dlna
{
    public class MediaProfile
    {
        [XmlAttribute("container")]
        public string Container { get; set; }

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; }

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        [XmlAttribute("orgPn")]
        public string OrgPn { get; set; }

        [XmlAttribute("mimeType")]
        public string MimeType { get; set; }

        public ProfileCondition[] Conditions { get; set; }

        public MediaProfile()
        {
            Conditions = new ProfileCondition[] {};
        }

        public List<string> GetContainers()
        {
            return (Container ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
        
        public List<string> GetAudioCodecs()
        {
            return (AudioCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }

        public List<string> GetVideoCodecs()
        {
            return (VideoCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }
}
