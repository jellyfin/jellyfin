#nullable enable

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Provides session-scoped additional audio filters for FFmpeg transcoding.
/// Plugins can inject per-play-session filters (for example volume expressions).
/// </summary>
public interface ISessionAudioFilterProvider
{
    /// <summary>
    /// Returns an additional FFmpeg audio filter string for the given play session, or null if none.
    /// The filter is appended to the existing audio filter chain.
    /// </summary>
    /// <param name="playSessionId">The play session ID from the request, or null if not available.</param>
    /// <param name="deviceId">The device ID from the request; use as fallback when playSessionId does not match.</param>
    /// <param name="startTimeSeconds">
    /// Stream start offset in seconds (from <c>-ss</c> / StartTimeTicks). Absolute media times
    /// must be shifted into FFmpeg filter time <c>t</c>, which resets after input seeking.
    /// </param>
    /// <returns>Filter string or null.</returns>
    string? GetAdditionalAudioFilter(string? playSessionId, string? deviceId, double startTimeSeconds = 0);
}
