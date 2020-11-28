using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class QueueRequestBody.
    /// </summary>
    public class QueueRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueRequestBody"/> class.
        /// </summary>
        public QueueRequestBody()
        {
            ItemIds = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets or sets the items to enqueue.
        /// </summary>
        /// <value>The items to enqueue.</value>
        public IReadOnlyList<Guid> ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the mode in which to add the new items.
        /// </summary>
        /// <value>The enqueue mode.</value>
        public GroupQueueMode Mode { get; set; }
    }
}
