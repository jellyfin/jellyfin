using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelMediaInfo
    {
        public string Path { get; set; }

        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public string Container { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }

        public int? AudioBitrate { get; set; }
        public int? VideoBitrate { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? AudioChannels { get; set; }

        public ChannelMediaInfo()
        {
            RequiredHttpHeaders = new Dictionary<string, string>();
        }
    }
}