#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class PlaybackStoppedEventArgs : EventArgs
    {
        public PlaybackStoppedEventArgs(uBaseObject mediaInfo)
        {
            MediaInfo = mediaInfo ?? throw new ArgumentNullException(nameof(mediaInfo));
        }

        public uBaseObject MediaInfo { get; }
    }
}
