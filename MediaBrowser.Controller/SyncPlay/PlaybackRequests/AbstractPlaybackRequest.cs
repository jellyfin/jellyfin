using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests
{
    /// <summary>
    /// Class AbstractPlaybackRequest.
    /// </summary>
    public abstract class AbstractPlaybackRequest : IGroupPlaybackRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPlaybackRequest"/> class.
        /// </summary>
        protected AbstractPlaybackRequest()
        {
            // Do nothing.
        }

        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.Playback;

        /// <inheritdoc />
        public abstract PlaybackRequestType Action { get; }

        /// <inheritdoc />
        public abstract void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken);
    }
}
