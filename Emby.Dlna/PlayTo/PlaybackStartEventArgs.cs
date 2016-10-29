using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}
