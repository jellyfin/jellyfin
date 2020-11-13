using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Class SetRepeatModeGroupRequest.
    /// </summary>
    public class SetRepeatModeGroupRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public string Mode { get; set; }

        /// <inheritdoc />
        public PlaybackRequestType Type { get; } = PlaybackRequestType.SetRepeatMode;

        /// <inheritdoc />
        public void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(context, state.Type, this, session, cancellationToken);
        }
    }
}
