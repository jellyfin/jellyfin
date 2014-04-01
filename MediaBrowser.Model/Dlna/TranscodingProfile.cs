using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class TranscodingProfile
    {
        [XmlAttribute("container")]
        public string Container { get; set; }

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; }

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; }

        [XmlAttribute("protocol")]
        public string Protocol { get; set; }
        
        [XmlAttribute("estimateContentLength")]
        public bool EstimateContentLength { get; set; }

        [XmlAttribute("enableMpegtsM2TsMode")]
        public bool EnableMpegtsM2TsMode { get; set; }

        [XmlAttribute("transcodeSeekInfo")]
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        [XmlAttribute("videoProfile")]
        public string VideoProfile { get; set; }

        public List<string> GetAudioCodecs()
        {
            return (AudioCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrEmpty(i)).ToList();
        }
    }

    public enum TranscodeSeekInfo
    {
        Auto = 0,
        Bytes = 1
    }
}
