#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class PlayRequest.
    /// </summary>
    public class PlayRequest
    {
        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        public Guid[] ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks that the first item should be played at.
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
        public Guid ControllingUserId { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? AudioStreamIndex { get; set; }

        public string MediaSourceId { get; set; }

        public int? StartIndex { get; set; }
    }
}
