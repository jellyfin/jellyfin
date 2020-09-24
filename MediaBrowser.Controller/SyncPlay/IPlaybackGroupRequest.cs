using System.Threading;
using MediaBrowser.Model.SyncPlay;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.SyncPlay
{
    /// <summary>
    /// Interface IPlaybackGroupRequest.
    /// </summary>
    public interface IPlaybackGroupRequest
    {
        /// <summary>
        /// Gets the playback request type.
        /// </summary>
        /// <returns>The playback request type.</returns>
        PlaybackRequestType GetRequestType();

        /// <summary>
        /// Applies the request to a group.
        /// </summary>
        void Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken);
    }
}
