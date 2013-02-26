using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api.Playback
{
    public class StreamRequest
    {
        public string Id { get; set; }

        public AudioCodecs? AudioCodec { get; set; }

        public long? StartTimeTicks { get; set; }

        public int? AudioBitRate { get; set; }

        public VideoCodecs? VideoCodec { get; set; }

        public int? VideoBitRate { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? VideoStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? AudioChannels { get; set; }

        public int? AudioSampleRate { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }

        public int? MaxWidth { get; set; }

        public int? MaxHeight { get; set; }

        public double? Framerate { get; set; }
    }
}
