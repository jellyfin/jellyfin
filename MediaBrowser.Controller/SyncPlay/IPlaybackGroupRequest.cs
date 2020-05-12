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
        /// <value>The playback request type.</value>
        PlaybackRequestType Type();

        /// <summary>
        /// Applies the request to a group.
        /// </summary>
        /// <value>The operation completion status.</value>
        bool Apply(ISyncPlayStateContext context, ISyncPlayState state, SessionInfo session, CancellationToken cancellationToken);
    }
}
