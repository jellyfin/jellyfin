#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class PlaybackStoppedEventArgs : EventArgs
    {
        public UBaseObject MediaInfo { get; set; }
    }
}
