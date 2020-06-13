#nullable disable
#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class PlaybackInfoRequest
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public long? MaxStreamingBitrate { get; set; }

        public long? StartTimeTicks { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public string MediaSourceId { get; set; }

        public string LiveStreamId { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public bool EnableDirectPlay { get; set; }
        public bool EnableDirectStream { get; set; }
        public bool EnableTranscoding { get; set; }
        public bool AllowVideoStreamCopy { get; set; }
        public bool AllowAudioStreamCopy { get; set; }
        public bool IsPlayback { get; set; }
        public bool AutoOpenLiveStream { get; set; }

        public MediaProtocol[] DirectPlayProtocols { get; set; }

        public PlaybackInfoRequest()
        {
            EnableDirectPlay = true;
            EnableDirectStream = true;
            EnableTranscoding = true;
            AllowVideoStreamCopy = true;
            AllowAudioStreamCopy = true;
            IsPlayback = true;
            DirectPlayProtocols = new MediaProtocol[] { MediaProtocol.Http };
        }
    }
}
