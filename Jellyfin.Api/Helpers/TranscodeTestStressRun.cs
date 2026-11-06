using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Models.TranscodeTestDtos;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// The streams, phase and running totals of one stress test.
/// </summary>
internal sealed class TranscodeTestStressRun
{
    private readonly object _lock = new();
    private double _nextSample;
    private int _lastStreamId;
    private double? _nextStreamAt;
    private int? _failedStreamId;
    private double? _failedStreamFps;
    private int _streamsAtEnd;
    private string? _error;
    private int? _behindStreamId;
    private double? _endsAt;
    private bool _timeLimitReached;
    private bool _stopped;

    public TranscodeTestStressRun(int maxStreams)
    {
        MaxStreams = maxStreams;
    }

    public int MaxStreams { get; }

    public List<TranscodeTestStream> Streams { get; } = [];

    public TranscodeTestStressPhase Phase { get; private set; } = TranscodeTestStressPhase.Starting;

    public TranscodeTestEndReason EndReason { get; private set; } = TranscodeTestEndReason.None;

    public double ThresholdFps { get; set; }

    public int MaxStable { get; set; }

    public int PeakStreams { get; private set; }

    public double PeakCombinedFps { get; private set; }

    public TranscodeTestMediaInfo? Input { get; set; }

    public TranscodeTestOutputInfo? Output { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the source is shorter than <see cref="TranscodeTestManager.ShortSourceSeconds"/>,
    /// so streams restart often.
    /// </summary>
    public bool ShortSource { get; set; }

    /// <summary>
    /// Gets or sets the folder the streams write to.
    /// </summary>
    public string? Folder { get; set; }

    /// <summary>
    /// Gets or sets the start of the names of all the files the streams write.
    /// </summary>
    public string? FilePrefix { get; set; }

    public int NextStreamId() => ++_lastStreamId;

    /// <summary>
    /// Adds a stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns><c>false</c> when the run was stopped meanwhile; the caller then stops the stream.</returns>
    public bool Add(TranscodeTestStream stream)
    {
        lock (_lock)
        {
            if (_stopped)
            {
                return false;
            }

            Streams.Add(stream);
            PeakStreams = Math.Max(PeakStreams, Streams.Count(s => !s.HasExited));
            return true;
        }
    }

    /// <summary>
    /// Stops accepting streams and gets all of them, so every process is seen by whoever stops them, whichever thread
    /// adds or stops one.
    /// </summary>
    /// <returns>Every stream added.</returns>
    public IReadOnlyList<TranscodeTestStream> Stop()
    {
        lock (_lock)
        {
            _stopped = true;
            return Streams.ToArray();
        }
    }

    /// <summary>
    /// Sets the phase and, while every stream keeps up, when the next stream is due.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <param name="nextStreamAt">Seconds since the start when the next stream is due, if any.</param>
    public void SetPhase(TranscodeTestStressPhase phase, double? nextStreamAt)
    {
        lock (_lock)
        {
            Phase = phase;
            _nextStreamAt = nextStreamAt;
            _behindStreamId = null;
            _endsAt = null;
        }
    }

    /// <summary>
    /// Marks the test as falling behind.
    /// </summary>
    /// <param name="streamId">The stream that has been behind the longest.</param>
    /// <param name="endsAt">Seconds since the start when the test ends unless it catches up.</param>
    public void SetFallingBehind(int streamId, double endsAt)
    {
        lock (_lock)
        {
            Phase = TranscodeTestStressPhase.FallingBehind;
            _nextStreamAt = null;
            _behindStreamId = streamId;
            _endsAt = endsAt;
        }
    }

    /// <summary>
    /// Marks the test as ended, keeping the first reason given.
    /// </summary>
    /// <param name="reason">Why it ended.</param>
    /// <param name="now">Seconds since the test started.</param>
    /// <param name="streamId">The stream that fell behind or failed.</param>
    /// <param name="fps">The frame rate of the stream that fell behind.</param>
    /// <param name="error">The error, if any.</param>
    /// <param name="timeLimitReached">Whether the test ran for its time limit.</param>
    public void End(TranscodeTestEndReason reason, double now, int? streamId = null, double? fps = null, string? error = null, bool timeLimitReached = false)
    {
        lock (_lock)
        {
            if (EndReason != TranscodeTestEndReason.None)
            {
                return;
            }

            var running = Streams.Where(s => !s.HasExited).ToList();
            foreach (var s in running)
            {
                s.FinalFps = s.Fps(now) is double f ? TranscodeTestRules.RoundFps(f, ThresholdFps) : null;
            }

            _streamsAtEnd = running.Count;
            EndReason = reason;
            Phase = TranscodeTestStressPhase.Finished;
            _nextStreamAt = null;
            _behindStreamId = null;
            _endsAt = null;
            _failedStreamId = streamId;
            _failedStreamFps = fps.HasValue ? TranscodeTestRules.RoundFps(fps.Value, ThresholdFps) : null;
            _error = error;
            _timeLimitReached = timeLimitReached;
        }
    }

    /// <summary>
    /// Takes the once-per-second samples and collects finished segments on every call, so the transcode folder holds
    /// only a few segments per stream however fast the streams write.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    public void Sample(double now)
    {
        if (now >= _nextSample)
        {
            _nextSample = now + 1;
            var live = Streams.Where(s => !s.HasExited).ToList();
            foreach (var s in live)
            {
                s.Sample(now);
            }

            var combined = live.Sum(s => s.Fps(now) ?? 0);
            PeakCombinedFps = Math.Max(PeakCombinedFps, combined);
        }

        foreach (var s in Streams.Where(s => !s.HasExited))
        {
            s.CollectSegments();
        }
    }

    /// <summary>
    /// Gets the progress and results so far.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    /// <returns>The stress test result.</returns>
    public TranscodeTestStressResult Snapshot(double now)
    {
        lock (_lock)
        {
            var finished = Phase == TranscodeTestStressPhase.Finished;
            // A stream started again at the end of the file is shown as its newest process, which carries the output
            // of the ones before it, so the average is taken over the shown streams.
            var shown = Streams.GroupBy(s => s.Id).Select(g => g.Last()).ToList();
            var rates = shown.Select(s => s.OutputMbps).Where(r => r.HasValue).Select(r => r!.Value).ToList();
            return new TranscodeTestStressResult
            {
                Phase = Phase,
                EndReason = EndReason,
                RunningStreams = finished ? 0 : Streams.Count(s => !s.HasExited),
                MaxStableStreams = MaxStable,
                PeakStreams = PeakStreams,
                MaxStreams = MaxStreams,
                NextStreamInSeconds = _nextStreamAt.HasValue ? Math.Max(0, Math.Round(_nextStreamAt.Value - now, 1)) : null,
                BehindStreamId = _behindStreamId,
                EndsInSeconds = _endsAt.HasValue ? Math.Max(0, Math.Round(_endsAt.Value - now, 1)) : null,
                RampSeconds = TranscodeTestManager.RampSeconds,
                SettleSeconds = TranscodeTestStream.WindowSeconds,
                FailSeconds = TranscodeTestManager.FailSeconds,
                ThresholdFps = ThresholdFps,
                FpsRatio = TranscodeTestManager.FpsRatio,
                FailedStreamId = _failedStreamId,
                FailedStreamFps = _failedStreamFps,
                StreamsAtEnd = _streamsAtEnd,
                TimeLimitReached = _timeLimitReached,
                PeakCombinedFps = Math.Round(PeakCombinedFps, 1),
                AverageOutputMbps = rates.Count > 0 ? Math.Round(rates.Average(), 2) : null,
                Error = _error,
                Input = Input,
                Output = Output,
                ShortSource = ShortSource,
                Streams = shown.Select(s => new TranscodeTestStreamResult
                {
                    Id = s.Id,
                    Fps = !s.HasExited && s.Fps(now) is double f ? TranscodeTestRules.RoundFps(f, ThresholdFps) : null,
                    FinalFps = s.FinalFps,
                    MedianFps = s.MedianFps,
                    OutputMbps = s.OutputMbps,
                    Status = s.HasExited ? (s.Stopping || s.ExitCode == 0 ? TranscodeTestStreamStatus.Stopped : TranscodeTestStreamStatus.Failed) : TranscodeTestStreamStatus.Running
                }).ToArray()
            };
        }
    }
}
