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

        /// <summary>
        /// Gets or sets the WebVTT cue settings (e.g. "line:10% align:center").
        /// Populated from the cue timing line when parsing VTT files.
        /// </summary>
        public string? VttCueSettings { get; set; }
    }
}
