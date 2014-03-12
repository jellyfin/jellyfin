
using System;
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Enum PlaystateCommand
    /// </summary>
    public enum PlaystateCommand
    {
        /// <summary>
        /// The stop
        /// </summary>
        Stop,
        /// <summary>
        /// The pause
        /// </summary>
        Pause,
        /// <summary>
        /// The unpause
        /// </summary>
        Unpause,
        /// <summary>
        /// The next track
        /// </summary>
        NextTrack,
        /// <summary>
        /// The previous track
        /// </summary>
        PreviousTrack,
        /// <summary>
        /// The seek
        /// </summary>
        Seek
    }

    public class PlaystateRequest
    {
        public Guid UserId { get; set; }

        public PlaystateCommand Command { get; set; }

        public long? SeekPositionTicks { get; set; }
    }
}
