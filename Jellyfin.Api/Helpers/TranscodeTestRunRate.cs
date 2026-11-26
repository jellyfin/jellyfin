namespace Jellyfin.Api.Helpers;

/// <summary>
/// A stress test process's frame rate over its whole run, from its first frame to its last progress line.
/// </summary>
/// <param name="Fps">The frame rate.</param>
/// <param name="From">When the first frame came, in seconds since the test started.</param>
/// <param name="To">When the last progress line came, in seconds since the test started.</param>
internal sealed record TranscodeTestRunRate(double Fps, double From, double To);
