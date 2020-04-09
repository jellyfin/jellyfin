#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveStreamRequest
    {
        public LiveStreamRequest()
        {
            EnableDirectPlay = true;
            EnableDirectStream = true;
            DirectPlayProtocols = new MediaProtocol[] { MediaProtocol.Http };
        }

        public LiveStreamRequest(AudioOptions options)
        {
            MaxStreamingBitrate = options.MaxBitrate;
            ItemId = options.ItemId;
            DeviceProfile = options.Profile;
            MaxAudioChannels = options.MaxAudioChannels;

            DirectPlayProtocols = new MediaProtocol[] { MediaProtocol.Http };

            if (options is VideoOptions videoOptions)
            {
                AudioStreamIndex = videoOptions.AudioStreamIndex;
                SubtitleStreamIndex = videoOptions.SubtitleStreamIndex;
            }
        }

        public string OpenToken { get; set; }
        public Guid UserId { get; set; }
        public string PlaySessionId { get; set; }
        public long? MaxStreamingBitrate { get; set; }
        public long? StartTimeTicks { get; set; }
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
        public int? MaxAudioChannels { get; set; }
        public Guid ItemId { get; set; }
        public DeviceProfile DeviceProfile { get; set; }

        public bool EnableDirectPlay { get; set; }
        public bool EnableDirectStream { get; set; }
        public MediaProtocol[] DirectPlayProtocols { get; set; }
    }
}
