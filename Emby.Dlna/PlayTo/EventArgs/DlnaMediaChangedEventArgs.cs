#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo.EventArgs
{
    public class DlnaMediaChangedEventArgs
    {
        public UBaseObject OldMediaInfo { get; set; }

        public UBaseObject NewMediaInfo { get; set; }
    }
}
