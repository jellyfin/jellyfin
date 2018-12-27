using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class SubtitleTrackInfo
    {
        public SubtitleTrackEvent[] TrackEvents { get; set; }

        public SubtitleTrackInfo()
        {
            TrackEvents = new SubtitleTrackEvent[] { };
        }
    }
}
