#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveStreamRequest
    {
        public LiveStreamRequest()
        {
            EnableDirectPlay = true;
            EnableDirectStream = true;
            AlwaysBurnInSubtitleWhenTranscoding = false;
            DirectPlayProtocols = new MediaProtocol[] { MediaProtocol.Http };
        }

        public string OpenToken { get; set; }

        public Guid UserId { get; set; }

        public string PlaySessionId { get; set; }

        public int? MaxStreamingBitrate { get; set; }

        public long? StartTimeTicks { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public Guid ItemId { get; set; }

        public DeviceProfile DeviceProfile { get; set; }

        public bool EnableDirectPlay { get; set; }

        public bool EnableDirectStream { get; set; }

        public bool AlwaysBurnInSubtitleWhenTranscoding { get; set; }

        public IReadOnlyList<MediaProtocol> DirectPlayProtocols { get; set; }
    }
}
