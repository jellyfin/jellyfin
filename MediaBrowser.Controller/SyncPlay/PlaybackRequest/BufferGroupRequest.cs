using System;
using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class BufferingGroupRequest.
    /// </summary>
    public class BufferGroupRequest : IPlaybackGroupRequest
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
        /// Gets or sets the playing item id.
        /// </summary>
        /// <value>The playing item id.</value>
        public Guid PlayingItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type()
        {
            return PlaybackRequestType.Buffer;
        }

        /// <inheritdoc />
        public bool Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            return state.HandleRequest(context, false, this, session, cancellationToken);
        }
    }
}
