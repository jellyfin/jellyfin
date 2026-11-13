using MediaBrowser.Controller.LiveTv;

namespace Jellyfin.LiveTv.Recordings;

/// <summary>
/// Applies timer changes to recordings that are already running.
/// </summary>
public interface IActiveRecordingUpdater
{
    /// <summary>
    /// Applies a changed timer to its recording, if that timer is currently being recorded.
    /// </summary>
    /// <param name="timer">The changed timer.</param>
    /// <returns><c>true</c> if the timer is being recorded; otherwise, <c>false</c>.</returns>
    bool TryUpdateTimer(TimerInfo timer);
}
