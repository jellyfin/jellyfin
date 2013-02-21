using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class MediaStream
    /// </summary>
    [ProtoContract]
    public class MediaStream
    {
        /// <summary>
        /// Gets or sets the codec.
        /// </summary>
        /// <value>The codec.</value>
        [ProtoMember(1)]
        public string Codec { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        [ProtoMember(2)]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the bit rate.
        /// </summary>
        /// <value>The bit rate.</value>
        [ProtoMember(3)]
        public int? BitRate { get; set; }

        /// <summary>
        /// Gets or sets the channels.
        /// </summary>
        /// <value>The channels.</value>
        [ProtoMember(4)]
        public int? Channels { get; set; }

        /// <summary>
        /// Gets or sets the sample rate.
        /// </summary>
        /// <value>The sample rate.</value>
        [ProtoMember(5)]
        public int? SampleRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is default.
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        [ProtoMember(6)]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is forced.
        /// </summary>
        /// <value><c>true</c> if this instance is forced; otherwise, <c>false</c>.</value>
        [ProtoMember(7)]
        public bool IsForced { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        [ProtoMember(8)]
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        [ProtoMember(9)]
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the type of the scan.
        /// </summary>
        /// <value>The type of the scan.</value>
        [ProtoMember(10)]
        public string ScanType { get; set; }

        /// <summary>
        /// Gets or sets the average frame rate.
        /// </summary>
        /// <value>The average frame rate.</value>
        [ProtoMember(11)]
        public float? AverageFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the real frame rate.
        /// </summary>
        /// <value>The real frame rate.</value>
        [ProtoMember(12)]
        public float? RealFrameRate { get; set; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        /// <value>The profile.</value>
        [ProtoMember(13)]
        public string Profile { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(14)]
        public MediaStreamType Type { get; set; }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>The aspect ratio.</value>
        [ProtoMember(15)]
        public string AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [ProtoMember(16)]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is external.
        /// </summary>
        /// <value><c>true</c> if this instance is external; otherwise, <c>false</c>.</value>
        [ProtoMember(17)]
        public bool IsExternal { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        [ProtoMember(18)]
        public string Path { get; set; }
    }

    /// <summary>
    /// Enum MediaStreamType
    /// </summary>
    public enum MediaStreamType
    {
        /// <summary>
        /// The audio
        /// </summary>
        Audio,
        /// <summary>
        /// The video
        /// </summary>
        Video,
        /// <summary>
        /// The subtitle
        /// </summary>
        Subtitle
    }
}
