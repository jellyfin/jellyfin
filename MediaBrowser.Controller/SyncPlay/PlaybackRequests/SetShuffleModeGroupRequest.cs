using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SetShuffleModeGroupRequest.
    /// </summary>
    public class SetShuffleModeGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public string Mode { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.SetShuffleMode;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
