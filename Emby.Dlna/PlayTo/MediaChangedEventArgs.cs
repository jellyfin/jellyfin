#pragma warning disable CS1591

namespace Emby.Dlna.PlayTo
{
    public class MediaChangedEventArgs
    {
        public UBaseObject OldMediaInfo { get; set; }

        public UBaseObject NewMediaInfo { get; set; }
    }
}
