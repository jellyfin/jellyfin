using System;
using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class PlayGroupRequest.
    /// </summary>
    public class PlayGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the playing queue.
        /// </summary>
        /// <value>The playing queue.</value>
        public Guid[] PlayingQueue { get; set; }

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
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.Play;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
