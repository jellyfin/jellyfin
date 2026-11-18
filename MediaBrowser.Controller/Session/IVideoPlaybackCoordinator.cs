namespace MediaBrowser.Controller.Session;

/// <summary>
/// Coordinates logical video playback admissions independently of HTTP and encoding jobs.
/// </summary>
public interface IVideoPlaybackCoordinator
{
    /// <summary>
    /// Binds an encoding identifier to a verified logical playback without consuming quota.
    /// </summary>
    /// <param name="playback">The server-verified playback context.</param>
    /// <param name="playSessionId">The unique encoding identifier, never reused for another epoch.</param>
    /// <returns>The registration result.</returns>
    /// <remarks>
    /// Callers resolve legacy requests to a canonical context before registration.
    /// A missing client identifier must not bypass admission. A session-and-item
    /// fallback cannot distinguish multiple players of the same item on that session.
    /// </remarks>
    VideoPlaybackStatus RegisterPlaySession(VideoPlaybackContext playback, string playSessionId);

    /// <summary>
    /// Acquires one operation, atomically reserving quota for a new logical playback.
    /// </summary>
    /// <param name="playback">The server-verified playback context.</param>
    /// <param name="playSessionId">A registered encoding identifier.</param>
    /// <param name="maxActiveVideoStreams">The authoritative user limit; zero means unlimited.</param>
    /// <param name="request">The operation handle when admission succeeds.</param>
    /// <returns>The admission result.</returns>
    VideoPlaybackStatus TryAcquire(
        VideoPlaybackContext playback,
        string playSessionId,
        int maxActiveVideoStreams,
        out IVideoPlaybackRequest? request);

    /// <summary>
    /// Renews an existing admission from an authenticated, real activity report.
    /// </summary>
    /// <param name="playback">The server-verified playback context.</param>
    /// <param name="playSessionId">A registered encoding identifier.</param>
    /// <returns>The renewal result.</returns>
    VideoPlaybackStatus ReportActivity(VideoPlaybackContext playback, string playSessionId);

    /// <summary>
    /// Stops the current playback generation, retaining its quota until operations drain.
    /// </summary>
    /// <param name="playback">The server-verified playback context.</param>
    /// <param name="playSessionId">The encoding identifier reporting the stop.</param>
    /// <returns>The stop result; superseded encoding identifiers cannot stop newer ones.</returns>
    VideoPlaybackStatus Stop(VideoPlaybackContext playback, string playSessionId);

    /// <summary>
    /// Expires abandoned entries and removes old terminal encoding identifiers.
    /// </summary>
    /// <returns>The number of logical playback entries expired by this call.</returns>
    /// <remarks>
    /// Active operation handles prevent expiry. The caller schedules cleanup;
    /// this registry does not start timers or perform I/O.
    /// </remarks>
    int RemoveExpired();
}
