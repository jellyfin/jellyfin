
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlayRequest
    /// </summary>
    public class PlayRequest
    {
        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        public string[] ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks that the first item should be played at
        /// </summary>
        /// <value>The start position ticks.</value>
        public long? StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        public PlayCommand PlayCommand { get; set; }

        /// <summary>
        /// Gets or sets the controlling user identifier.
        /// </summary>
        /// <value>The controlling user identifier.</value>
        public string ControllingUserId { get; set; }
    }

    /// <summary>
    /// Enum PlayCommand
    /// </summary>
    public enum PlayCommand
    {
        /// <summary>
        /// The play now
        /// </summary>
        PlayNow = 0,
        /// <summary>
        /// The play next
        /// </summary>
        PlayNext = 1,
        /// <summary>
        /// The play last
        /// </summary>
        PlayLast = 2,
        /// <summary>
        /// The play instant mix
        /// </summary>
        PlayInstantMix = 3,
        /// <summary>
        /// The play shuffle
        /// </summary>
        PlayShuffle = 4
    }
}