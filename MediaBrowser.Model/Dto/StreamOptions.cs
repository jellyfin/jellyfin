namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class VideoStreamOptions
    /// </summary>
    public class VideoStreamOptions : StreamOptions
    {
        /// <summary>
        /// Gets or sets the video codec.
        /// Omit to copy
        /// </summary>
        /// <value>The video codec.</value>
        public VideoCodecs? VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the video bit rate.
        /// </summary>
        /// <value>The video bit rate.</value>
        public int? VideoBitRate { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width of the max.
        /// </summary>
        /// <value>The width of the max.</value>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the max.
        /// </summary>
        /// <value>The height of the max.</value>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the frame rate.
        /// </summary>
        /// <value>The frame rate.</value>
        public double? FrameRate { get; set; }

        /// <summary>
        /// Gets or sets the index of the audio stream.
        /// </summary>
        /// <value>The index of the audio stream.</value>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the video stream.
        /// </summary>
        /// <value>The index of the video stream.</value>
        public int? VideoStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the subtitle stream.
        /// </summary>
        /// <value>The index of the subtitle stream.</value>
        public int? SubtitleStreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public string Level { get; set; }

        /// <summary>
        /// Gets or sets the baseline stream audio bit rate.
        /// </summary>
        /// <value>The baseline stream audio bit rate.</value>
        public int? BaselineStreamAudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [append baseline stream].
        /// </summary>
        /// <value><c>true</c> if [append baseline stream]; otherwise, <c>false</c>.</value>
        public bool AppendBaselineStream { get; set; }
    }

    /// <summary>
    /// Class StreamOptions
    /// </summary>
    public class StreamOptions
    {
        /// <summary>
        /// Gets or sets the audio bit rate.
        /// </summary>
        /// <value>The audio bit rate.</value>
        public int? AudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// Omit to copy the original stream
        /// </summary>
        /// <value>The audio encoding format.</value>
        public AudioCodecs? AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the max audio channels.
        /// </summary>
        /// <value>The max audio channels.</value>
        public int? MaxAudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the max audio sample rate.
        /// </summary>
        /// <value>The max audio sample rate.</value>
        public int? MaxAudioSampleRate { get; set; }

        /// <summary>
        /// Gets or sets the start time ticks.
        /// </summary>
        /// <value>The start time ticks.</value>
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the original media should be served statically
        /// Only used with progressive streaming
        /// </summary>
        /// <value><c>true</c> if static; otherwise, <c>false</c>.</value>
        public bool? Static { get; set; }

        /// <summary>
        /// Gets or sets the output file extension.
        /// </summary>
        /// <value>The output file extension.</value>
        public string OutputFileExtension { get; set; }
    }

    /// <summary>
    /// These are the codecs the api is capable of encoding to
    /// </summary>
    public enum AudioCodecs
    {
        /// <summary>
        /// The aac
        /// </summary>
        Aac,
        /// <summary>
        /// The MP3
        /// </summary>
        Mp3,
        /// <summary>
        /// The vorbis
        /// </summary>
        Vorbis,
        /// <summary>
        /// The wma
        /// </summary>
        Wma,
        /// <summary>
        /// The copy
        /// </summary>
        Copy
    }

    /// <summary>
    /// Enum VideoCodecs
    /// </summary>
    public enum VideoCodecs
    {
        H263,

        /// <summary>
        /// The H264
        /// </summary>
        H264,

        /// <summary>
        /// The mpeg4
        /// </summary>
        Mpeg4,

        /// <summary>
        /// The theora
        /// </summary>
        Theora,

        /// <summary>
        /// The VPX
        /// </summary>
        Vpx,

        /// <summary>
        /// The WMV
        /// </summary>
        Wmv,
        /// <summary>
        /// The copy
        /// </summary>
        Copy
    }
}
