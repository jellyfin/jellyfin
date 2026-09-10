#nullable disable

using System.Threading;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.PlaybackRequests;

/// <summary>
/// Class SetPlaybackSpeedGroupRequest.
/// </summary>
public class SetPlaybackSpeedGroupRequest : AbstractPlaybackRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetPlaybackSpeedGroupRequest"/> class.
    /// </summary>
    /// <param name="playbackSpeed">The playback speed.</param>
    public SetPlaybackSpeedGroupRequest(double playbackSpeed)
    {
        PlaybackSpeed = playbackSpeed;
    }

    /// <summary>
    /// Gets the playback speed.
    /// </summary>
    /// <value>The playback speed.</value>
    public double PlaybackSpeed { get; }

    /// <inheritdoc />
    public override PlaybackRequestType Action { get; } = PlaybackRequestType.SetPlaybackSpeed;

    /// <inheritdoc />
    public override void Apply(IGroupStateContext context, IGroupState state, SessionInfo session, CancellationToken cancellationToken)
    {
        state.HandleRequest(this, context, state.Type, session, cancellationToken);
    }
}
