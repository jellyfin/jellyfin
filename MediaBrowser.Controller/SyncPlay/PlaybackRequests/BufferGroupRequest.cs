#nullable disable

using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class BufferGroupRequest.
    /// </summary>
    public class BufferGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferGroupRequest"/> class.
        /// </summary>
        /// <param name="when">When the request has been made, as reported by the client.</param>
        /// <param name="positionTicks">The position ticks.</param>
        /// <param name="isPlaying">Whether the client playback is unpaused.</param>
        /// <param name="playlistItemId">The playlist item identifier of the playing item.</param>
        public BufferGroupRequest(DateTime when, long positionTicks, bool isPlaying, Guid playlistItemId)
        {
            When = when;
            PositionTicks = positionTicks;
            IsPlaying = isPlaying;
            PlaylistItemId = playlistItemId;
        }

        /// <summary>
        /// Gets when the request has been made by the client.
        /// </summary>
        /// <value>The date of the request.</value>
        public DateTime When { get; }

        /// <summary>
        /// Gets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; }

        /// <summary>
        /// Gets a value indicating whether the client playback is unpaused.
        /// </summary>
        /// <value>The client playback status.</value>
        public bool IsPlaying { get; }

        /// <summary>
        /// Gets the playlist item identifier of the playing item.
        /// </summary>
        /// <value>The playlist item identifier.</value>
        public Guid PlaylistItemId { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.Buffer;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
