#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class QueueGroupRequest.
    /// </summary>
    public class QueueGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueGroupRequest"/> class.
        /// </summary>
        /// <param name="items">The items to add to the queue.</param>
        /// <param name="mode">The enqueue mode.</param>
        public QueueGroupRequest(IReadOnlyList<Guid> items, GroupQueueMode mode)
        {
            ItemIds = items ?? Array.Empty<Guid>();
            Mode = mode;
        }

        /// <summary>
        /// Gets the items to enqueue.
        /// </summary>
        /// <value>The items to enqueue.</value>
        public IReadOnlyList<Guid> ItemIds { get; }

        /// <summary>
        /// Gets the mode in which to add the new items.
        /// </summary>
        /// <value>The enqueue mode.</value>
        public GroupQueueMode Mode { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.Queue;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
