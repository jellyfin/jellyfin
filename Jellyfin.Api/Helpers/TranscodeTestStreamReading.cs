namespace Jellyfin.Api.Helpers;

/// <summary>
/// A running stream as the stress test controller sees it.
/// </summary>
/// <param name="Id">The stream number.</param>
/// <param name="Fps">The frame rate over the last few seconds, or <c>null</c> before it can be measured.</param>
/// <param name="AgeSeconds">Seconds since the stream started.</param>
internal sealed record TranscodeTestStreamReading(int Id, double? Fps, double AgeSeconds);
