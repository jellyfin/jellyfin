using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class PlaybackRequest.
    /// </summary>
    public class PlaybackRequest
    {
        /// <summary>
        /// Gets or sets the request type.
        /// </summary>
        /// <value>The request type.</value>
        public PlaybackRequestType Type { get; set; }

        /// <summary>
        /// Gets or sets when the request has been made by the client.
        /// </summary>
        /// <value>The date of the request.</value>
        public DateTime? When { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the ping time.
        /// </summary>
        /// <value>The ping time.</value>
        public long? Ping { get; set; }
    }
}
