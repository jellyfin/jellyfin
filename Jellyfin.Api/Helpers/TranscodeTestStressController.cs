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
/// until its first measurement. A stream is added at most every <see cref="RampSeconds"/>, once every running stream
/// has kept up for <see cref="SettleSeconds"/> since any of them was last below the threshold, so brief dips at the
/// limit delay the next stream without holding it back for good. A stream that stays below for
/// <see cref="FailSeconds"/> ends the test.
/// Only a stream that never reported a frame stalls; one that reported frames and then went quiet reads 0 fps, so it
/// gets the same grace as any stream that falls behind.
/// A stream started again at the end of the file is judged afresh: its time below the threshold starts over, and
/// until its new process is measured it neither counts as below nor holds back the next stream. A stream near the end
/// of its source is not judged either.
/// The test also ends after <see cref="TimeLimitSeconds"/>, so streams that keep dipping briefly cannot hold it at
/// full load for good.
/// </remarks>
internal sealed class TranscodeTestStressController
{
    private readonly Dictionary<int, double> _below = [];
    private readonly HashSet<int> _restarted = [];
    private readonly double _startedAt;
    private double _addedAt;
    private double? _belowAt;

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
    /// Records that a stream was added, which starts a new interval.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    public void StreamAdded(double now) => _addedAt = now;

    /// <summary>
    /// Records that a stream reached the end of the file and was started again with the same id. Its time below the
    /// threshold belonged to the old process, so it is forgotten.
    /// </summary>
    /// <param name="id">The stream number.</param>
    public void StreamRestarted(int id)
    {
        _below.Remove(id);
        _restarted.Add(id);
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

        foreach (var gone in _below.Keys.Where(id => streams.All(s => s.Id != id)).ToList())
        {
            _below.Remove(gone);
        }

        _restarted.RemoveWhere(id => streams.All(s => s.Id != id));

        var measuring = false;
        var wasBelow = _below.Count > 0;
        foreach (var s in streams)
        {
            if (s.NearEnd)
            {
                // ffmpeg drains and flushes at the end of the file; the stream restarts and is judged afresh.
                _below.Remove(s.Id);
                continue;
            }

            if (s.Fps is not double fps)
            {
                // A restarted stream was judged while its old process ran, so only a new stream holds back the next one.
                measuring |= !_restarted.Contains(s.Id);
                continue;
            }

            _restarted.Remove(s.Id);

            if (fps < ThresholdFps)
            {
                var since = _below.TryGetValue(s.Id, out var t) ? t : _below[s.Id] = now;
                if (now - since >= FailSeconds)
                {
                    return new(TranscodeTestStressAction.LimitReached, TranscodeTestStressPhase.Finished, StreamId: s.Id, Fps: fps);
                }
            }
            else
            {
                _below.Remove(s.Id);
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

        var nextStreamAt = Math.Max(_addedAt + RampSeconds, _belowAt is double belowAt ? belowAt + SettleSeconds : 0);
        if (measuring)
        {
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.WarmingUp, NextStreamAt: nextStreamAt);
        }

        if (now < nextStreamAt)
        {
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.KeepingUp, NextStreamAt: nextStreamAt);
        }

        MaxStable = Math.Max(MaxStable, streams.Count);
        return streams.Count >= MaxStreams
            ? new(TranscodeTestStressAction.MaxStreams, TranscodeTestStressPhase.Finished)
            : new(TranscodeTestStressAction.AddStream, TranscodeTestStressPhase.WarmingUp);
    }
}
