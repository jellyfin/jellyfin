using System;
using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class BufferGroupRequest.
    /// </summary>
    public class BufferGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets when the request has been made by the client.
        /// </summary>
        /// <value>The date of the request.</value>
        public DateTime When { get; set; }

        /// <summary>
        /// Gets or sets the position ticks.
        /// </summary>
        /// <value>The position ticks.</value>
        public long PositionTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client playback is unpaused.
        /// </summary>
        /// <value>The client playback status.</value>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the playlist item identifier of the playing item.
        /// </summary>
        /// <value>The playlist item identifier.</value>
        public string PlaylistItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.Buffer;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
