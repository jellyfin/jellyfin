using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class StreamRequest
    /// </summary>
    public class StreamRequest : BaseEncodingJobOptions
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "The media version id, if playing an alternate version", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }
        
        [ApiMember(Name = "DeviceId", Description = "The device id of the client requesting. Used to stop encoding processes when needed.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }

        [ApiMember(Name = "Container", Description = "Container", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        /// <value>The audio codec.</value>
        [ApiMember(Name = "AudioCodec", Description = "Optional. Specify a audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension. Options: aac, mp3, vorbis, wma.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AudioCodec { get; set; }

        public string SubtitleCodec { get; set; }

        [ApiMember(Name = "DeviceProfileId", Description = "Optional. The dlna device profile id to utilize.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceProfileId { get; set; }

        public string Params { get; set; }
        public string PlaySessionId { get; set; }
        public string LiveStreamId { get; set; }
        public string Tag { get; set; }
    }

    public class VideoStreamRequest : StreamRequest
    {
        /// <summary>
        /// Gets a value indicating whether this instance has fixed resolution.
        /// </summary>
        /// <value><c>true</c> if this instance has fixed resolution; otherwise, <c>false</c>.</value>
        public bool HasFixedResolution
        {
            get
            {
                return Width.HasValue || Height.HasValue;
            }
        }

        public bool EnableSubtitlesInManifest { get; set; }
    }
}
