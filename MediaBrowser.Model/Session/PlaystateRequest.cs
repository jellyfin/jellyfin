
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlaystateRequest
    /// </summary>
    public class PlaystateRequest
    {
        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The command.</value>
        public PlaystateCommand Command { get; set; }

        /// <summary>
        /// Gets or sets the seek position.
        /// Only applicable to seek commands.
        /// </summary>
        /// <value>The seek position.</value>
        public long SeekPosition { get; set; }
    }

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
}
