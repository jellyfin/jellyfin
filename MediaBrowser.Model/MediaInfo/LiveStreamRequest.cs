using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveStreamRequest
    {
        public string OpenToken { get; set; }
        public string UserId { get; set; }
        public int? MaxStreamingBitrate { get; set; }
        public long? StartTimeTicks { get; set; }
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
        public string ItemId { get; set; }
        public DeviceProfile DeviceProfile { get; set; }
    }
}
