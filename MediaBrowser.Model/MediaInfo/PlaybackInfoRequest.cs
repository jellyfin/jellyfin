using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class PlaybackInfoRequest
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public int? MaxStreamingBitrate { get; set; }

        public long? StartTimeTicks { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public string MediaSourceId { get; set; }

        public string LiveStreamId { get; set; }
        
        public DeviceProfile DeviceProfile { get; set; }
    }
}
