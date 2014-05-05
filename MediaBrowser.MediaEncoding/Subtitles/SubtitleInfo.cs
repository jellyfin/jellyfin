using System.Collections.Generic;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SubtitleInfo
    {
        public List<SubtitleTrackEvent> TrackEvents { get; set; }

        public SubtitleInfo()
        {
            TrackEvents = new List<SubtitleTrackEvent>();
        }
    }

    public class SubtitleTrackEvent
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public long StartPositionTicks { get; set; }
        public long EndPositionTicks { get; set; }
    }
}
