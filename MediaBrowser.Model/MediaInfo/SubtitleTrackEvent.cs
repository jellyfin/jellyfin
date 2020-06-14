#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.MediaInfo
{
    public class SubtitleTrackEvent
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public long StartPositionTicks { get; set; }

        public long EndPositionTicks { get; set; }
    }
}
