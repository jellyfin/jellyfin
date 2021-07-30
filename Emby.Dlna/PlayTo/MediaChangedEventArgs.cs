#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class MediaChangedEventArgs : EventArgs
    {
        public MediaChangedEventArgs(UBaseObject oldMediaInfo, UBaseObject newMediaInfo)
        {
            OldMediaInfo = oldMediaInfo;
            NewMediaInfo = newMediaInfo;
        }

        public UBaseObject OldMediaInfo { get; set; }

        public UBaseObject NewMediaInfo { get; set; }
    }
}
