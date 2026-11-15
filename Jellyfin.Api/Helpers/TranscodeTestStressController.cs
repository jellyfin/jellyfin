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
/// <see cref="SettleSeconds"/> after the last dip. Certification requires a simultaneously measured interval with
/// the same physical processes present throughout. Pending launches, drains and replacements invalidate that
/// interval. A completed process's average rate can establish a slowdown, but never concurrent capacity.
/// Only a stream that never reported a frame stalls; one that reported frames and then went quiet reads 0 fps, so it
/// gets the same grace as any stream that falls behind.
/// The test also ends after <see cref="TimeLimitSeconds"/>, so streams that keep dipping briefly cannot hold it at
/// full load for good.
/// </remarks>
internal sealed class TranscodeTestStressController
{
    private readonly Dictionary<int, double> _below = [];
    private readonly Dictionary<int, double?> _cohort = [];
    private readonly double _startedAt;
    private double _addedAt;
    private double _physicalSince;
    private double? _healthySince;
    private bool _physicalChanged;
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
        _physicalSince = now;
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
        _physicalSince = now;
        _healthySince = null;
        _cohort.Clear();
        _physicalChanged = false;
    }

    /// <summary>
    /// Records that a stream reached the end of the file and was started again with the same id. Its time below the
    /// threshold belonged to the old process, so it is forgotten. The old rate can prove a slowdown, but a past
    /// healthy rate cannot stand in for an absent physical process.
    /// </summary>
    /// <param name="id">The stream number.</param>
    /// <param name="run">The old process's rate from its first frame to its last progress line, with the times of
    /// both, or null when it was not measured.</param>
    public void StreamRestarted(int id, TranscodeTestRunRate? run = null)
    {
        _below.Remove(id);
        _healthySince = null;
        _physicalChanged = true;
        if (run is null || !double.IsFinite(run.Fps) || !double.IsFinite(run.From) || !double.IsFinite(run.To)
            || run.To <= run.From || run.From < _addedAt || run.Fps >= ThresholdFps)
        {
            return;
        }

        // A whole pass of the file below real time: the stream was behind for that long.
        _belowAt = Math.Max(_belowAt ?? run.To, run.To);
        var seconds = run.To - run.From;
        if (_slowRun is null || seconds > _slowRun.Value.Seconds)
        {
            _slowRun = (id, run.Fps, seconds);
        }
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

        var stalled = streams.FirstOrDefault(s => s.IsRunning && !s.ReportedFrames && s.Fps is null && s.AgeSeconds > StartTimeoutSeconds);
        if (stalled is not null)
        {
            return new(TranscodeTestStressAction.Stalled, TranscodeTestStressPhase.Finished, StreamId: stalled.Id);
        }

        if (_slowRun is (int slowId, double slowFps, double slowSeconds) && slowSeconds >= FailSeconds)
        {
            return new(TranscodeTestStressAction.LimitReached, TranscodeTestStressPhase.Finished, StreamId: slowId, Fps: slowFps);
        }

        _slowRun = null;
        var running = streams.Where(s => s.IsRunning).ToList();
        var changed = _physicalChanged || running.Count != _cohort.Count
            || running.Any(s => !_cohort.TryGetValue(s.Id, out var startedAt) || startedAt != s.ProcessStartedAt);
        if (changed)
        {
            // On first observation use the known start times. A replacement or a missing peer begins a new,
            // conservative epoch when observed, so a rolling rate spanning the old load cannot certify this one.
            _physicalSince = _physicalChanged || _cohort.Count > 0
                ? now
                : Math.Max(_physicalSince, running.Select(s => s.ProcessStartedAt ?? _addedAt).DefaultIfEmpty(_addedAt).Max());
            _healthySince = null;
            _cohort.Clear();
            foreach (var s in running)
            {
                _cohort[s.Id] = s.ProcessStartedAt;
            }

            _physicalChanged = false;
        }

        foreach (var gone in _below.Keys.Where(id => streams.All(s => s.Id != id)).ToList())
        {
            _below.Remove(gone);
        }

        var unmeasured = streams.Count == 0;
        var wasBelow = _below.Count > 0;
        var nextStreamAt = _addedAt + RampSeconds;
        foreach (var s in streams)
        {
            if (!s.IsRunning || s.NearEnd || s.Fps is not double fps || !double.IsFinite(fps))
            {
                // An absent or unmeasured peer is not evidence of the intended concurrent workload.
                // A missing measurement also breaks a sustained slowdown interval. Time with no evidence must
                // not turn two separate short dips into one measured performance failure.
                _below.Remove(s.Id);
                unmeasured = true;
                continue;
            }

            if (fps < ThresholdFps)
            {
                var since = _below.TryGetValue(s.Id, out var t) ? t : _below[s.Id] = now;
                if (now - since >= FailSeconds)
                {
                    return new(TranscodeTestStressAction.LimitReached, TranscodeTestStressPhase.Finished, StreamId: s.Id, Fps: fps);
                }

                continue;
            }

            _below.Remove(s.Id);
        }

        if (_below.Count > 0)
        {
            _healthySince = null;
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
            _healthySince = null;
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.WarmingUp, NextStreamAt: nextStreamAt);
        }

        if (now - _physicalSince < TranscodeTestStream.WindowSeconds)
        {
            _healthySince = null;
            nextStreamAt = Math.Max(nextStreamAt, _physicalSince + TranscodeTestStream.WindowSeconds + SettleSeconds);
            return new(TranscodeTestStressAction.Wait, TranscodeTestStressPhase.KeepingUp, NextStreamAt: nextStreamAt);
        }

        _healthySince ??= now;
        nextStreamAt = Math.Max(nextStreamAt, _healthySince.Value + SettleSeconds);
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
