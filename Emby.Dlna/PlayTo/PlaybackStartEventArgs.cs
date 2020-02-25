#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

namespace Emby.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
}
