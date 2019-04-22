using System;

namespace Jellyfin.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}
