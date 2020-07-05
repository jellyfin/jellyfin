#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class MediaChangedEventArgs : EventArgs
    {
        public MediaChangedEventArgs(uBaseObject oldmedia, uBaseObject newmedia)
        {
            OldMediaInfo = oldmedia ?? throw new ArgumentNullException(nameof(oldmedia));
            NewMediaInfo = newmedia ?? throw new ArgumentNullException(nameof(newmedia));
        }

        public uBaseObject OldMediaInfo { get; }

        public uBaseObject NewMediaInfo { get; }
    }
}
