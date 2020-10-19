#nullable disable
#pragma warning disable CS1591

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

        [XmlAttribute("copyTimestamps")]
        public bool CopyTimestamps { get; set; }

        [XmlAttribute("context")]
        public EncodingContext Context { get; set; }

        [XmlAttribute("enableSubtitlesInManifest")]
        public bool EnableSubtitlesInManifest { get; set; }

        [XmlAttribute("maxAudioChannels")]
        public string MaxAudioChannels { get; set; }

        [XmlAttribute("minSegments")]
        public int MinSegments { get; set; }

        [XmlAttribute("segmentLength")]
        public int SegmentLength { get; set; }

        [XmlAttribute("breakOnNonKeyFrames")]
        public bool BreakOnNonKeyFrames { get; set; }

        public string[] GetAudioCodecs()
        {
            return ContainerProfile.SplitValue(AudioCodec);
        }
    }
}
