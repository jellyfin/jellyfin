using MediaBrowser.Common.Events;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Holds information about a playback progress event
    /// </summary>
    public class PlaybackProgressEventArgs : GenericEventArgs<BaseItem>
    {
        public User User { get; set; }
        public long? PlaybackPositionTicks { get; set; }
    }
}
