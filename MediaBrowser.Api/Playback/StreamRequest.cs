using MediaBrowser.Model.Dto;
using ServiceStack.ServiceHost;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class StreamRequest
    /// </summary>
    public class StreamRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        /// <value>The audio codec.</value>
        [ApiMember(Name = "AudioCodec", Description = "Optional. Specify a specific audio codec to encode to, e.g. mp3. If omitted the server will attempt to infer it using the url's extension. Options: aac, mp3, vorbis, wma.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public AudioCodecs? AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the start time ticks.
        /// </summary>
        /// <value>The start time ticks.</value>
        [ApiMember(Name = "StartTimeTicks", Description = "Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public long? StartTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the audio bit rate.
        /// </summary>
        /// <value>The audio bit rate.</value>
        [ApiMember(Name = "AudioBitRate", Description = "Optional. Specify a specific audio bitrate to encode to, e.g. 128000", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AudioBitRate { get; set; }

        /// <summary>
        /// Gets or sets the audio channels.
        /// </summary>
        /// <value>The audio channels.</value>
        [ApiMember(Name = "AudioChannels", Description = "Optional. Specify a specific number of audio channels to encode to, e.g. 2", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AudioChannels { get; set; }

        /// <summary>
        /// Gets or sets the audio sample rate.
        /// </summary>
        /// <value>The audio sample rate.</value>
        [ApiMember(Name = "AudioSampleRate", Description = "Optional. Specify a specific audio sample rate, e.g. 44100", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AudioSampleRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="StreamRequest" /> is static.
        /// </summary>
        /// <value><c>true</c> if static; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "Static", Description = "Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool Static { get; set; }
    }

    public class VideoStreamRequest : StreamRequest
    {
        /// <summary>
        /// Gets or sets the video codec.
        /// </summary>
        /// <value>The video codec.</value>
        public VideoCodecs? VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the video bit rate.
        /// </summary>
        /// <value>The video bit rate.</value>
        public int? VideoBitRate { get; set; }

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
        /// Gets or sets the framerate.
        /// </summary>
        /// <value>The framerate.</value>
        public double? Framerate { get; set; }
    }
}
