using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PlayGroupRequest.
    /// </summary>
    public class PlayGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets the playing queue.
        /// </summary>
        /// <value>The playing queue.</value>
        public List<Guid> PlayingQueue { get; } = new List<Guid>();

        /// <summary>
        /// Gets or sets the playing item from the queue.
        /// </summary>
        /// <value>The playing item.</value>
        public int PlayingItemPosition { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Play;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
