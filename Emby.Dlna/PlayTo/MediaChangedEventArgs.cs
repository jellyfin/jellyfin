#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class MediaChangedEventArgs : EventArgs
    {
        public UBaseObject OldMediaInfo { get; set; }

        public UBaseObject NewMediaInfo { get; set; }
    }
}
