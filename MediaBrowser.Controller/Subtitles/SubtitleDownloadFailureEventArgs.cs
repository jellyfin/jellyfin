using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Subtitles
{
    public class SubtitleDownloadFailureEventArgs : EventArgs
    {
        public BaseItem Item { get; set; }

        public string Provider { get; set; }

        public Exception Exception { get; set; }
    }
}
