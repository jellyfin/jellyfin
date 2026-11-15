using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Models.TranscodeTestDtos;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Decides, from the streams' frame rates, when the stress test adds a stream and when it ends.
/// </summary>
/// <remarks>
/// Every measured stream is judged on every tick, also right after a stream is added; only a new stream is excused
/// until its first measurement. A stream that stays below the threshold for <see cref="FailSeconds"/> ends the test.
/// A stream is added at most every <see cref="RampSeconds"/>, once every stream has kept up for a full
/// <see cref="SettleSeconds"/> at the current load and none is below the threshold, and not before
/// <see cref="SettleSeconds"/> after the last dip. Each stream earns this on its own: a measured interval at or above
/// the threshold, or a whole process that reached the end of the file at or above it. A stream that already kept up
/// at this load and is now unmeasured, because it drains at the end of the file or is starting again, neither holds
/// back the next stream nor counts as below. A stream that has not kept up at this load yet holds it back, so a stream
/// that is never measured cannot certify capacity.
/// Only a stream that never reported a frame stalls; one that reported frames and then went quiet reads 0 fps, so it
/// gets the same grace as any stream that falls behind.
/// The test also ends after <see cref="TimeLimitSeconds"/>, so streams that keep dipping briefly cannot hold it at
/// full load for good.
/// </remarks>
internal sealed class TranscodeTestStressController
{
    private readonly Dictionary<int, double> _below = [];
    private readonly Dictionary<int, double> _keptUpSince = [];
    private readonly HashSet<int> _keptUp = [];
    private readonly double _startedAt;
    private double _addedAt;
    private double? _belowAt;
    private (int Id, double Fps, double Seconds)? _slowRun;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeTestStressController"/> class.
    /// </summary>
    /// <param name="thresholdFps">The frame rate each stream must keep.</param>
    /// <param name="rampSeconds">The least time between two added streams.</param>
    /// <param name="failSeconds">How long a stream may stay below the threshold.</param>
    /// <param name="startTimeoutSeconds">How long a stream may take to produce its first frames.</param>
    /// <param name="maxStreams">The most streams to run.</param>
    /// <param name="now">Seconds since the test started, when the first stream started.</param>
    /// <param name="settleSeconds">How long every stream must keep up after a dip before a stream is added.</param>
    /// <param name="timeLimitSeconds">How long the test may run at most, or null for no limit.</param>
    public TranscodeTestStressController(double thresholdFps, double rampSeconds, double failSeconds, double startTimeoutSeconds, int maxStreams, double now, double settleSeconds = TranscodeTestStream.WindowSeconds, double? timeLimitSeconds = null)
    {
        if (!double.IsFinite(thresholdFps) || thresholdFps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdFps));
        }

        TimeLimitSeconds = timeLimitSeconds;
        _startedAt = now;
        ThresholdFps = thresholdFps;
        RampSeconds = rampSeconds;
        SettleSeconds = settleSeconds;
        FailSeconds = failSeconds;
        StartTimeoutSeconds = startTimeoutSeconds;
        MaxStreams = maxStreams;
        _addedAt = now;
    }

    public double ThresholdFps { get; }

    public double RampSeconds { get; }

    public double SettleSeconds { get; }

    public double FailSeconds { get; }

    public double StartTimeoutSeconds { get; }

    public int MaxStreams { get; }

    /// <summary>
    /// Gets how long the test may run at most, from the first stream's start, or null for no limit.
    /// </summary>
    public double? TimeLimitSeconds { get; }

    /// <summary>
    /// Gets the most streams that all kept up for a full interval.
    /// </summary>
    public int MaxStable { get; private set; }

    /// <summary>
    /// Records that a stream was added, which starts a new interval: every stream must keep up again at the new load.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    public void StreamAdded(double now)
    {
        _addedAt = now;
        _keptUp.Clear();
        _keptUpSince.Clear();
    }

    /// <summary>
    /// Records that a stream reached the end of the file and was started again with the same id. Its time below the
    /// threshold belonged to the old process, so it is forgotten. The old process's rate over its whole run counts
    /// as a measurement when the whole run was at the current load.
    /// </summary>
    /// <param name="id">The stream number.</param>
    /// <param name="run">The old process's rate from its first frame to its last progress line, with the times of
    /// both, or null when it was not measured.</param>
    public void StreamRestarted(int id, TranscodeTestRunRate? run = null)
    {
        _below.Remove(id);
        _keptUpSince.Remove(id);
        if (run is null || !double.IsFinite(run.Fps) || run.From < _addedAt)
        {
            return;
        }

        if (run.Fps >= ThresholdFps)
        {
            _keptUp.Add(id);
            return;
        }

        // A whole pass of the file below real time: the stream was behind for that long.
        _keptUp.Remove(id);
        _belowAt = Math.Max(_belowAt ?? run.To, run.To);
        _slowRun ??= (id, run.Fps, run.To - run.From);
    }

    /// <summary>
    /// Judges the running streams.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    /// <param name="streams">The running streams.</param>
    /// <returns>What the test should do next.</returns>
    public TranscodeTestStressDecision Tick(double now, IReadOnlyList<TranscodeTestStreamReading> streams)
    {
        ArgumentNullException.ThrowIfNull(streams);
        if (TimeLimitSeconds is double limit && now - _startedAt >= limit)
        {
            return new(TranscodeTestStressAction.TimeLimit, TranscodeTestStressPhase.Finished);
        }

        var stalled = streams.FirstOrDefault(s => !s.ReportedFrames && s.Fps is null && s.AgeSeconds > StartTimeoutSeconds);
        if (stalled is not null)
        {
            return new(TranscodeTestStressAction.Stalled, TranscodeTestStressPhase.Finished, StreamId: stalled.Id);
        }

        if (_slowRun is (int slowId, double slowFps, double slowSeconds) && slowSeconds >= FailSeconds)
        {
            return new(TranscodeTestStressAction.LimitReached, TranscodeTestStressPhase.Finished, StreamId: slowId, Fps: slowFps);
        }

        _slowRun = null;
        foreach (var gone in _below.Keys.Where(id => streams.All(s => s.Id != id)).ToList())
        {
            _below.Remove(gone);
        }

        var unmeasured = streams.Count == 0;
        var wasBelow = _below.Count > 0;
        var nextStreamAt = _addedAt + RampSeconds;
        foreach (var s in streams)
        {
            if (s.NearEnd || s.Fps is not double fps || !double.IsFinite(fps))
            {
                // Draining at the end of the file, starting again, or not measured yet: only a stream that has not
                // kept up at this load yet holds back the next one.
                if (s.NearEnd)
                {
                    _below.Remove(s.Id);
                }

                _keptUpSince.Remove(s.Id);
                unmeasured |= !_keptUp.Contains(s.Id);
                continue;
            }

            if (fps < ThresholdFps)
            {
                _keptUp.Remove(s.Id);
                _keptUpSince.Remove(s.Id);
                var since = _below.TryGetValue(s.Id, out var t) ? t : _below[s.Id] = now;
                if (now - since >= FailSeconds)
                {
                    return new(TranscodeTestStressAction.LimitReached, TranscodeTestStressPhase.Finished, StreamId: s.Id, Fps: fps);
                }

                continue;
            }

            _below.Remove(s.Id);
            var keptUpSince = _keptUpSince.TryGetValue(s.Id, out var k) ? k : _keptUpSince[s.Id] = now;
            if (now - keptUpSince >= SettleSeconds)
            {
                _keptUp.Add(s.Id);
            }
            else if (!_keptUp.Contains(s.Id))
            {
                nextStreamAt = Math.Max(nextStreamAt, keptUpSince + SettleSeconds);
            }
        }

        if (_below.Count > 0)
        {
            // Someone is behind: say so now; the next stream waits until all have kept up for the settle time.
            var (behindId, since) = _below.MinBy(b => b.Value);
            _belowAt = now;
            return new(
                TranscodeTestStressAction.Wait,
                TranscodeTestStressPhase.FallingBehind,
                StreamId: behindId,
                Fps: streams.First(s => s.Id == behindId).Fps,
                EndsAt: since + FailSeconds,
                DipStarted: !wasBelow);
        }

        if (_belowAt is double belowAt)
        {
            nextStreamAt = Math.Max(nextStreamAt, belowAt + SettleSeconds);
        }

        if (unmeasured)
        {
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.WarmingUp, NextStreamAt: nextStreamAt);
        }

        if (now < nextStreamAt || streams.Any(s => !_keptUp.Contains(s.Id)))
        {
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.KeepingUp, NextStreamAt: nextStreamAt);
        }

        MaxStable = Math.Max(MaxStable, streams.Count);
        return streams.Count >= MaxStreams
            ? new(TranscodeTestStressAction.MaxStreams, TranscodeTestStressPhase.Finished)
            : new(TranscodeTestStressAction.AddStream, TranscodeTestStressPhase.WarmingUp);
    }
}
