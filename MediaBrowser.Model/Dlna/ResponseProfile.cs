using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.Dlna
{
    public class ResponseProfile
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

        public ResponseProfile()
        {
            Conditions = new ProfileCondition[] {};
        }

        public string[] GetContainers()
        {
            return ContainerProfile.SplitValue(Container);
        }

        public string[] GetAudioCodecs()
        {
            return ContainerProfile.SplitValue(AudioCodec);
        }

        public string[] GetVideoCodecs()
        {
            return ContainerProfile.SplitValue(VideoCodec);
        }
    }
}
