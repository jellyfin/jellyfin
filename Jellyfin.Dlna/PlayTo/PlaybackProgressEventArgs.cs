using System;

namespace Jellyfin.Dlna.PlayTo
{
    public class PlaybackProgressEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}
