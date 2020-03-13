#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}
