using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveStreamRequest
    {
        public string OpenToken { get; set; }
        public string UserId { get; set; }
        public string PlaySessionId { get; set; }
        public long? MaxStreamingBitrate { get; set; }
        public long? StartTimeTicks { get; set; }
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
        public int? MaxAudioChannels { get; set; }
        public string ItemId { get; set; }
        public DeviceProfile DeviceProfile { get; set; }

        public bool EnableDirectPlay { get; set; }
        public bool EnableDirectStream { get; set; }
        public bool EnableMediaProbe { get; set; }

        public LiveStreamRequest()
        {
            EnableDirectPlay = true;
            EnableDirectStream = true;
            EnableMediaProbe = true;
        }

        public LiveStreamRequest(AudioOptions options)
        {
            MaxStreamingBitrate = options.MaxBitrate;
            ItemId = options.ItemId;
            DeviceProfile = options.Profile;
            MaxAudioChannels = options.MaxAudioChannels;

            VideoOptions videoOptions = options as VideoOptions;
            if (videoOptions != null)
            {
                AudioStreamIndex = videoOptions.AudioStreamIndex;
                SubtitleStreamIndex = videoOptions.SubtitleStreamIndex;
            }
        }
    }
}
