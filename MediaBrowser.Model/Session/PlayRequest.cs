
using System;
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlayRequest
    /// </summary>
    public class PlayRequest
    {
        public PlayRequest()
        {

        }

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
        /// Gets or sets the play command.
        /// </summary>
        /// <value>The play command.</value>
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Enum PlayCommand
    /// </summary>
    public enum PlayCommand
    {
        /// <summary>
        /// The play now
        /// </summary>
        PlayNow,
        /// <summary>
        /// The play next
        /// </summary>
        PlayNext,
        /// <summary>
        /// The play last
        /// </summary>
        PlayLast
    }
}
