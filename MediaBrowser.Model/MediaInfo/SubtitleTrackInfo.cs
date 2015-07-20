using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class SubtitleTrackInfo
    {
        public List<SubtitleTrackEvent> TrackEvents { get; set; }

        public SubtitleTrackInfo()
        {
            TrackEvents = new List<SubtitleTrackEvent>();
        }
    }
}
