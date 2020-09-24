using System;
using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class QueueGroupRequest.
    /// </summary>
    public class QueueGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the items to queue.
        /// </summary>
        /// <value>The items to queue.</value>
        public Guid[] ItemIds { get; set; }

        /// <summary>
        /// Gets or sets the mode in which to add the new items.
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.Queue;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
