#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Enum PlaystateCommand.
    /// </summary>
    public enum PlaystateCommand
    {
        /// <summary>
        /// The stop.
        /// </summary>
        Stop,

        /// <summary>
        /// The pause.
        /// </summary>
        Pause,

        /// <summary>
        /// The unpause.
        /// </summary>
        Unpause,

        /// <summary>
        /// The next track.
        /// </summary>
        NextTrack,

        /// <summary>
        /// The previous track.
        /// </summary>
        PreviousTrack,

        /// <summary>
        /// The seek.
        /// </summary>
        Seek,

        /// <summary>
        /// The rewind.
        /// </summary>
        Rewind,

        /// <summary>
        /// The fast forward.
        /// </summary>
        FastForward,
        PlayPause
    }
}
