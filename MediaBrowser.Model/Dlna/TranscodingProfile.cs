using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="TranscodingProfile" />.
    /// </summary>
    public class TranscodingProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingProfile"/> class.
        /// </summary>
        public TranscodingProfile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <param name="audioCodec">The audio codec.</param>
        public TranscodingProfile(string container, string videoCodec, string audioCodec)
        {
            Container = container;
            VideoCodec = videoCodec;
            AudioCodec = audioCodec;
            Type = DlnaProfileType.Video;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="audioCodec">The audio codec.</param>
        public TranscodingProfile(string? container, string? audioCodec)
        {
            Container = container;
            AudioCodec = audioCodec;
            Type = DlnaProfileType.Audio;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodingProfile"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public TranscodingProfile(string container)
        {
            Container = container;
            Type = DlnaProfileType.Photo;
        }

        /// <summary>
        /// Gets or sets the Container.
        /// </summary>
        [XmlAttribute("container")]
        public string? Container { get; set; }

        /// <summary>
        /// Gets or sets the Dlna Profile Type..
        /// </summary>
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        /// <summary>
        /// Gets or sets the video Codec..
        /// </summary>
        [XmlAttribute("videoCodec")]
        public string? VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the audio codec..
        /// </summary>
        [XmlAttribute("audioCodec")]
        public string? AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the Protocol.
        /// </summary>
        [XmlAttribute("protocol")]
        public string? Protocol { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content length should be estimated..
        /// </summary>
        [XmlAttribute("estimateContentLength")]
        public bool EstimateContentLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MpegtsM2TsMode is enabled..
        /// </summary>
        [XmlAttribute("enableMpegtsM2TsMode")]
        public bool EnableMpegtsM2TsMode { get; set; }

        /// <summary>
        /// Gets or sets the transcode seek information..
        /// </summary>
        [XmlAttribute("transcodeSeekInfo")]
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the timestamps should be copied..
        /// </summary>
        [XmlAttribute("copyTimestamps")]
        public bool CopyTimestamps { get; set; }

        /// <summary>
        /// Gets or sets the encoding context..
        /// </summary>
        [XmlAttribute("context")]
        public EncodingContext Context { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subtitles in the manifest should be enabled..
        /// </summary>
        [XmlAttribute("enableSubtitlesInManifest")]
        public bool EnableSubtitlesInManifest { get; set; }

        /// <summary>
        /// Gets or sets the maximum audio channels..
        /// </summary>
        [XmlAttribute("maxAudioChannels")]
        public string? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the minimum segments..
        /// </summary>
        [XmlAttribute("minSegments")]
        public int MinSegments { get; set; }

        /// <summary>
        /// Gets or sets the segment length..
        /// </summary>
        [XmlAttribute("segmentLength")]
        public int SegmentLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether break on non-key frames is enabled..
        /// </summary>
        [XmlAttribute("breakOnNonKeyFrames")]
        public bool BreakOnNonKeyFrames { get; set; }

        /// <summary>
        /// Gets the audio codecs.
        /// </summary>
        /// <returns>A string array containing the audio codecs.</returns>
        public string[] GetAudioCodecs()
        {
            return ContainerProfile.SplitValue(AudioCodec ?? string.Empty);
        }
    }
}
