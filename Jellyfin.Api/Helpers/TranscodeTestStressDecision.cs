using Jellyfin.Api.Models.TranscodeTestDtos;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// What the stress test should do next.
/// </summary>
/// <param name="Action">The action.</param>
/// <param name="Phase">The phase to show.</param>
/// <param name="StreamId">The stream that is behind, failed or stalled.</param>
/// <param name="Fps">The frame rate of the stream that fell behind.</param>
/// <param name="NextStreamAt">When the next stream is due, while waiting.</param>
/// <param name="EndsAt">When the test ends unless the stream catches up, while falling behind.</param>
/// <param name="DipStarted">Whether a stream fell below the threshold on this tick while none was before, which starts
/// the wait for the next stream again.</param>
internal sealed record TranscodeTestStressDecision(
    TranscodeTestStressAction Action,
    TranscodeTestStressPhase Phase,
    int? StreamId = null,
    double? Fps = null,
    double? NextStreamAt = null,
    double? EndsAt = null,
    bool DipStarted = false);
