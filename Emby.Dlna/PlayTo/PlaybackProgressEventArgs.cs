using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaybackProgressEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}