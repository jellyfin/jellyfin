using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class DirectPlayProfile
    {
        [XmlAttribute("container")]
        public string Container { get; set; }

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; }

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        public bool SupportsContainer(string container)
        {
            return ContainerProfile.ContainsContainer(Container, container);
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
