#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class PlayGroupRequest.
    /// </summary>
    public class PlayGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayGroupRequest"/> class.
        /// </summary>
        /// <param name="playingQueue">The playing queue.</param>
        /// <param name="playingItemPosition">The playing item position.</param>
        /// <param name="startPositionTicks">The start position ticks.</param>
        public PlayGroupRequest(IReadOnlyList<Guid> playingQueue, int playingItemPosition, long startPositionTicks)
        {
            PlayingQueue = playingQueue ?? Array.Empty<Guid>();
            PlayingItemPosition = playingItemPosition;
            StartPositionTicks = startPositionTicks;
        }

        /// <summary>
        /// Gets the playing queue.
        /// </summary>
        /// <value>The playing queue.</value>
        public IReadOnlyList<Guid> PlayingQueue { get; }

        /// <summary>
        /// Gets the position of the playing item in the queue.
        /// </summary>
        /// <value>The playing item position.</value>
        public int PlayingItemPosition { get; }

        /// <summary>
        /// Gets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.Play;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
