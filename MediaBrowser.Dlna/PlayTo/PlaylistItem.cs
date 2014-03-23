using MediaBrowser.Controller.Dlna;
using System.Collections.Generic;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaylistItem
    {
        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }
        
        public bool Transcode { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public int PlayState { get; set; }

        public string StreamUrl { get; set; }

        public string Didl { get; set; }

        public long StartPositionTicks { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }

        public List<TranscodingSetting> TranscodingSettings { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }
        
        public PlaylistItem()
        {
            TranscodingSettings = new List<TranscodingSetting>();
        }
    }
}