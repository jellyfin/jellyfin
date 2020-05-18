#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class SubtitleTrackInfo
    {
        public SubtitleTrackInfo()
        {
            TrackEvents = Array.Empty<SubtitleTrackEvent>();
        }

        public IReadOnlyList<SubtitleTrackEvent> TrackEvents { get; set; }
    }
}
