using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Holds information about a playback progress event
    /// </summary>
    public class PlaybackProgressEventArgs : EventArgs
    {
        public User User { get; set; }
        public long? PlaybackPositionTicks { get; set; }
        public BaseItem Item { get; set; }
    }
}
