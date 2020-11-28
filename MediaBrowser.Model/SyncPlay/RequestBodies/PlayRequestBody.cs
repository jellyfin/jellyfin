using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class PlayRequestBody.
    /// </summary>
    public class PlayRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayRequestBody"/> class.
        /// </summary>
        public PlayRequestBody()
        {
            PlayingQueue = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets or sets the playing queue.
        /// </summary>
        /// <value>The playing queue.</value>
        public IReadOnlyList<Guid> PlayingQueue { get; set; }

        /// <summary>
        /// Gets or sets the position of the playing item in the queue.
        /// </summary>
        /// <value>The playing item position.</value>
        public int PlayingItemPosition { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }
    }
}
