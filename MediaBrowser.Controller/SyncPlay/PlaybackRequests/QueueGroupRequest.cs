using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class QueueGroupRequest.
    /// </summary>
    public class QueueGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets the items to queue.
        /// </summary>
        /// <value>The items to queue.</value>
        public List<Guid> ItemIds { get; } = new List<Guid>();

        /// <summary>
        /// Gets or sets the mode in which to add the new items.
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Queue;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
