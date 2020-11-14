using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class SetShuffleModeGroupRequest.
    /// </summary>
    public class SetShuffleModeGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetShuffleModeGroupRequest"/> class.
        /// </summary>
        /// <param name="mode">The shuffle mode.</param>
        public SetShuffleModeGroupRequest(GroupShuffleMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// Gets the shuffle mode.
        /// </summary>
        /// <value>The shuffle mode.</value>
        public GroupShuffleMode Mode { get; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.SetShuffleMode;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
