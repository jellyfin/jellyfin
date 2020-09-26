#pragma warning disable CS1591

namespace Emby.Dlna.PlayTo
{
    public class PlaybackEventArgs
    {
        public PlaybackEventArgs(UBaseObject mediaInfo)
        {
            MediaInfo = mediaInfo;
        }

        public UBaseObject MediaInfo { get; }
    }
}
