using System;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlaybackStartEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }

    public class PlaybackProgressEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }

    public class PlaybackStoppedEventArgs : EventArgs
    {
        public uBaseObject MediaInfo { get; set; }
    }
    
    public enum TRANSPORTSTATE
    {
        STOPPED,
        PLAYING,
        TRANSITIONING,
        PAUSED_PLAYBACK,
        PAUSED
    }
}
