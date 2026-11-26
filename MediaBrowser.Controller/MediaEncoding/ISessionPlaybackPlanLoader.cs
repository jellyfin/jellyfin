#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Optional plugin service that loads per-session playback state before the first FFmpeg request
/// checks for an audio filter or edit graph.
/// </summary>
public interface ISessionPlaybackPlanLoader
{
    /// <summary>
    /// Ensures any plugin playback plan for this play session is loaded.
    /// Called immediately before checking for a session audio filter or edit graph.
    /// </summary>
    /// <param name="playSessionId">Play session ID from the stream request (may be null or empty).</param>
    /// <param name="deviceId">Device ID from the stream request (may be null or empty).</param>
    /// <param name="itemId">Item ID from the stream request (may be null).</param>
    /// <param name="mediaSourceId">Media source id from the stream request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsurePlanLoadedAsync(
        string? playSessionId,
        string? deviceId,
        string? itemId,
        string? mediaSourceId,
        CancellationToken cancellationToken);
}
