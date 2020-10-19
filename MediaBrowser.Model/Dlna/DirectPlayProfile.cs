#nullable disable
#pragma warning disable CS1591

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

        public bool SupportsVideoCodec(string codec)
        {
            return Type == DlnaProfileType.Video && ContainerProfile.ContainsContainer(VideoCodec, codec);
        }

        public bool SupportsAudioCodec(string codec)
        {
            return (Type == DlnaProfileType.Audio || Type == DlnaProfileType.Video) && ContainerProfile.ContainsContainer(AudioCodec, codec);
        }
    }
}
