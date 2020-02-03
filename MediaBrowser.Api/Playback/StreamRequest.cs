using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class StreamRequest
    /// </summary>
    public class StreamRequest : BaseEncodingJobOptions
    {
        [ApiMember(Name = "DeviceProfileId", Description = "Optional. The dlna device profile id to utilize.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceProfileId { get; set; }

        public string Params { get; set; }
        public string PlaySessionId { get; set; }
        public string Tag { get; set; }
        public string SegmentContainer { get; set; }

        public int? SegmentLength { get; set; }
        public int? MinSegments { get; set; }
    }

    public class VideoStreamRequest : StreamRequest
    {
        /// <summary>
        /// Gets a value indicating whether this instance has fixed resolution.
        /// </summary>
        /// <value><c>true</c> if this instance has fixed resolution; otherwise, <c>false</c>.</value>
        public bool HasFixedResolution => Width.HasValue || Height.HasValue;

        public bool EnableSubtitlesInManifest { get; set; }
    }
}
