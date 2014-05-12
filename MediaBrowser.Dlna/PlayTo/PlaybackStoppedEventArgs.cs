using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaybackStoppedEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}