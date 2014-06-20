using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaybackStoppedEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }

    public class MediaChangedEventArgs : EventArgs
    {
        public uBaseObject OldMediaInfo { get; set; }
        public uBaseObject NewMediaInfo { get; set; }
    }
}