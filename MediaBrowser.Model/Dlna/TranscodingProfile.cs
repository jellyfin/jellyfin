#pragma warning disable CS1591

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Dlna
{
    public class TranscodingProfile
    {
        public TranscodingProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
        }

        [XmlAttribute("container")]
        public string Container { get; set; } = string.Empty;

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; } = string.Empty;

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; } = string.Empty;

        [XmlAttribute("protocol")]
        public MediaStreamProtocol Protocol { get; set; } = MediaStreamProtocol.http;

        [DefaultValue(false)]
        [XmlAttribute("estimateContentLength")]
        public bool EstimateContentLength { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("enableMpegtsM2TsMode")]
        public bool EnableMpegtsM2TsMode { get; set; }

        [DefaultValue(TranscodeSeekInfo.Auto)]
        [XmlAttribute("transcodeSeekInfo")]
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("copyTimestamps")]
        public bool CopyTimestamps { get; set; }

        [DefaultValue(EncodingContext.Streaming)]
        [XmlAttribute("context")]
        public EncodingContext Context { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("enableSubtitlesInManifest")]
        public bool EnableSubtitlesInManifest { get; set; }

        [XmlAttribute("maxAudioChannels")]
        public string? MaxAudioChannels { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("minSegments")]
        public int MinSegments { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("segmentLength")]
        public int SegmentLength { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("breakOnNonKeyFrames")]
        public bool BreakOnNonKeyFrames { get; set; }

        public ProfileCondition[] Conditions { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("enableAudioVbrEncoding")]
        public bool EnableAudioVbrEncoding { get; set; }

        public string[] GetAudioCodecs()
        {
            return ContainerProfile.SplitValue(AudioCodec);
        }
    }
}
