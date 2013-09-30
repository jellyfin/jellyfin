using MediaBrowser.Controller.Entities;
using System;

namespace MediaBrowser.Controller.Session
{
    public class PlaybackStopInfo
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        /// <value>The session id.</value>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long? PositionTicks { get; set; }
    }
}
