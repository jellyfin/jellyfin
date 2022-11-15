#pragma warning disable CS1591

namespace MediaBrowser.Model.MediaInfo
{
    public class SubtitleTrackEvent
    {
        public SubtitleTrackEvent(string id, string text)
        {
            Id = id;
            Text = text;
        }

        public string Id { get; set; }

        public string Text { get; set; }

        public long StartPositionTicks { get; set; }

        public long EndPositionTicks { get; set; }
    }
}
