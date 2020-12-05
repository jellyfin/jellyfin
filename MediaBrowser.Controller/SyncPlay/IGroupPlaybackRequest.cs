using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface IGroupPlaybackRequest.
    /// </summary>
    public interface IGroupPlaybackRequest : ISyncPlayRequest
    {
        /// <summary>
        /// Gets the playback request type.
        /// </summary>
        /// <returns>The playback request type.</returns>
        PlaybackRequestType Action { get; }

        /// <summary>
        /// Applies the request to a group.
        /// </summary>
        /// <param name="context">The context of the state.</param>
        /// <param name="state">The current state.</param>
        /// <param name="session">The session.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken);
    }
}
