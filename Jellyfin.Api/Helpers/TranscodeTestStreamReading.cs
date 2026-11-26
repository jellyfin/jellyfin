namespace Jellyfin.Api.Helpers;

/// <summary>
/// A running stream as the stress test controller sees it.
/// </summary>
/// <param name="Id">The stream number.</param>
/// <param name="Fps">The frame rate over the last few seconds, or <c>null</c> before it can be measured.</param>
/// <param name="AgeSeconds">Seconds since the stream started.</param>
/// <param name="ReportedFrames">Whether ffmpeg reported at least one encoded frame.</param>
/// <param name="NearEnd">Whether the stream is so close to the end of its source that it is not judged.</param>
/// <param name="IsRunning">Whether the physical process is present, rather than awaiting a launch or restart.</param>
/// <param name="ProcessStartedAt">The physical process's start time, distinguishing replacements of the same id.</param>
internal sealed record TranscodeTestStreamReading(int Id, double? Fps, double AgeSeconds, bool ReportedFrames, bool NearEnd = false, bool IsRunning = true, double? ProcessStartedAt = null);
