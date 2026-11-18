using System;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Holds one HTTP or shared operation on an admitted video playback.
/// </summary>
public interface IVideoPlaybackRequest : IDisposable
{
    /// <summary>
    /// Confirms successful playback preparation or media delivery.
    /// </summary>
    /// <remarks>
    /// Confirmation preserves the logical admission between operations. Disposing
    /// this handle ends only its operation, not the entire playback.
    /// </remarks>
    void Acknowledge();

    /// <summary>
    /// Holds an independent operation on the same playback, such as a shared job.
    /// </summary>
    /// <returns>A separately disposable handle.</returns>
    /// <exception cref="InvalidOperationException">The playback has already stopped.</exception>
    IVideoPlaybackRequest Retain();
}
