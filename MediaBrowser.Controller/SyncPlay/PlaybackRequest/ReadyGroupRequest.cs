using System;
using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class BufferingDoneGroupRequest.
    /// </summary>
    public class ReadyGroupRequest : IPlaybackGroupRequest
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
        /// Gets or sets the client playback status.
        /// </summary>
        /// <value>The client playback status.</value>
        public bool IsPlaying { get; set; }

        /// <summary>
        /// Gets or sets the playlist item id of the playing item.
        /// </summary>
        /// <value>The playlist item id.</value>
        public string PlaylistItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.Ready;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
