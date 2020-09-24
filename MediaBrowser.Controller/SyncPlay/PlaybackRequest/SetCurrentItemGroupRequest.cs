using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SetPlaylistItemGroupRequest.
    /// </summary>
    public class SetPlaylistItemGroupRequest : IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets or sets the playlist id of the playing item.
        /// </summary>
        /// <value>The playlist id of the playing item.</value>
        public string PlaylistItemId { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType GetRequestType()
        {
            return PlaybackRequestType.SetPlaylistItem;
        }

        /// <inheritdoc />
        public void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.GetGroupState(), this, session, cancellationToken);
        }
    }
}
