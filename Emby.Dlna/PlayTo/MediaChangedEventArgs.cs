#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class MediaChangedEventArgs
    {
        public MediaChangedEventArgs(UBaseObject previousMediaInfo, UBaseObject mediaInfo)
        {
            OldMediaInfo = previousMediaInfo;
            NewMediaInfo = mediaInfo;
        }

        public UBaseObject OldMediaInfo { get; }

        public UBaseObject NewMediaInfo { get; }
    }
}
