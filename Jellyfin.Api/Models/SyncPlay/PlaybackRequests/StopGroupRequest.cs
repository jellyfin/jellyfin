using System.Threading;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Api.Models.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class StopGroupRequest.
    /// </summary>
    public class StopGroupRequest : AbstractPlaybackRequest
    {
        /// <inheritdoc />
        public override PlaybackRequestType Action { get; } = PlaybackRequestType.Stop;

        /// <inheritdoc />
        public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
        {
            state.HandleRequest(this, context, state.Type, session, cancellationToken);
        }
    }
}
