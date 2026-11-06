using System.Collections.Generic;

namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The progress and result of a stress test.
/// </summary>
public class TranscodeTestStressResult
{
    /// <summary>
    /// Gets or sets where the test is.
    /// </summary>
    public TranscodeTestStressPhase Phase { get; set; }

    /// <summary>
    /// Gets or sets why the test ended.
    /// </summary>
    public TranscodeTestEndReason EndReason { get; set; }

    /// <summary>
    /// Gets or sets the streams running now.
    /// </summary>
    public int RunningStreams { get; set; }

    /// <summary>
    /// Gets or sets the most streams that all kept up.
    /// </summary>
    public int MaxStableStreams { get; set; }

    /// <summary>
    /// Gets or sets the most streams that ran at once.
    /// </summary>
    public int PeakStreams { get; set; }

    /// <summary>
    /// Gets or sets the stream limit.
    /// </summary>
    public int MaxStreams { get; set; }

    /// <summary>
    /// Gets or sets the seconds until the next stream is added, while every stream keeps up.
    /// </summary>
    public double? NextStreamInSeconds { get; set; }

    /// <summary>
    /// Gets or sets the stream that is furthest behind, while falling behind.
    /// </summary>
    public int? BehindStreamId { get; set; }

    /// <summary>
    /// Gets or sets the seconds until the test ends unless the stream catches up, while falling behind.
    /// </summary>
    public double? EndsInSeconds { get; set; }

    /// <summary>
    /// Gets or sets the least time between two added streams.
    /// </summary>
    public double RampSeconds { get; set; }

    /// <summary>
    /// Gets or sets how long every running stream must have kept up, after any of them dipped, before one is added:
    /// the window over which a stream's frame rate is measured.
    /// </summary>
    public double SettleSeconds { get; set; }

    /// <summary>
    /// Gets or sets how long a stream may stay below the threshold.
    /// </summary>
    public double FailSeconds { get; set; }

    /// <summary>
    /// Gets or sets how long a stream may take to report its first frame.
    /// </summary>
    public double StartTimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the frame rate each stream must keep.
    /// </summary>
    public double ThresholdFps { get; set; }

    /// <summary>
    /// Gets or sets the threshold as a fraction of the source frame rate.
    /// </summary>
    public double FpsRatio { get; set; }

    /// <summary>
    /// Gets or sets the source frame rate. A stream's speed is its frame rate divided by this; 1 is real time.
    /// </summary>
    public double SourceFps { get; set; }

    /// <summary>
    /// Gets or sets the median frame rate of the first stream while it ran alone.
    /// </summary>
    public double? SingleStreamFps { get; set; }

    /// <summary>
    /// Gets or sets <see cref="SingleStreamFps"/> as a multiple of real time.
    /// </summary>
    public double? SingleStreamSpeed { get; set; }

    /// <summary>
    /// Gets or sets why the test ended, as the Jellyfin hardware survey (hwa.jellyfin.org) names it. Null for an
    /// ending the survey has no name for.
    /// </summary>
    public TranscodeTestSurveyFailureReason? SurveyFailureReason { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test ended because the newest stream, added after at least one
    /// stream kept up, produced no frames, by stalling or by exiting before its first frame: the server could not
    /// start one more stream.
    /// </summary>
    public bool CouldNotStartStream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stream that produced no frames had not opened the file and used
    /// little CPU time, so it was probably waiting on reading the media rather than on the GPU.
    /// </summary>
    public bool WaitingOnInput { get; set; }

    /// <summary>
    /// Gets or sets the stream that fell behind or failed.
    /// </summary>
    public int? FailedStreamId { get; set; }

    /// <summary>
    /// Gets or sets the frame rate of the stream that fell behind.
    /// </summary>
    public double? FailedStreamFps { get; set; }

    /// <summary>
    /// Gets or sets how many streams were running when the test ended.
    /// </summary>
    public int StreamsAtEnd { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the test ended because it ran for its time limit, rather than because a
    /// stream fell behind for good. The kept-up count is the result.
    /// </summary>
    public bool TimeLimitReached { get; set; }

    /// <summary>
    /// Gets or sets the highest combined frame rate.
    /// </summary>
    public double PeakCombinedFps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether, when the limit was reached, the streams all slowed down sharply at once
    /// rather than one falling a little behind, which points at the GPU's memory or driver rather than its speed.
    /// </summary>
    public bool Collapsed { get; set; }

    /// <summary>
    /// Gets or sets the average output bitrate per stream, in megabits per second.
    /// </summary>
    public double? AverageOutputMbps { get; set; }

    /// <summary>
    /// Gets or sets the error, when the test could not run or ffmpeg failed: ffmpeg's error lines, never its other output.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public TranscodeTestMediaInfo? Input { get; set; }

    /// <summary>
    /// Gets or sets what each stream produces.
    /// </summary>
    public TranscodeTestOutputInfo? Output { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source is shorter than a minute. Each stream restarts at the end of
    /// the file, which briefly disturbs the measurement, so a longer file gives steadier results.
    /// </summary>
    public bool ShortSource { get; set; }

    /// <summary>
    /// Gets or sets the streams.
    /// </summary>
    public IReadOnlyList<TranscodeTestStreamResult> Streams { get; set; } = [];
}
