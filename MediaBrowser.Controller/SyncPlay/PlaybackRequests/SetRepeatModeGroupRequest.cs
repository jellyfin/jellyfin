using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class SetRepeatModeGroupRequest.
    /// </summary>
    public class SetRepeatModeGroupRequest : AbstractPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetRepeatModeGroupRequest"/> class.
        /// </summary>
        /// <param name="mode">The repeat mode.</param>
        public SetRepeatModeGroupRequest(GroupRepeatMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// Gets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode Mode { get; }

        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.SetRepeatMode;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
