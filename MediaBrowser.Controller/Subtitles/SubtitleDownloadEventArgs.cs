using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Subtitles
{
    public class SubtitleDownloadEventArgs
    {
        public BaseItem Item { get; set; }

        public string Format { get; set; }

        public string Language { get; set; }

        public bool IsForced { get; set; }

        public string Provider { get; set; }
    }

    public class SubtitleDownloadFailureEventArgs
    {
        public BaseItem Item { get; set; }

        public string Provider { get; set; }

        public Exception Exception { get; set; }
    }
}
