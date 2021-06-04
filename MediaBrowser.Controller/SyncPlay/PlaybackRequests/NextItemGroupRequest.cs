#nullable disable

using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class NextItemGroupRequest.
    /// </summary>
    public class NextItemGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextItemGroupRequest"/> class.
        /// </summary>
        /// <param name="playlistItemId">The playing item identifier.</param>
        public NextItemGroupRequest(Guid playlistItemId)
        {
            PlaylistItemId = playlistItemId;
        }

        /// <summary>
        /// Gets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public Guid PlaylistItemId { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.NextItem;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
