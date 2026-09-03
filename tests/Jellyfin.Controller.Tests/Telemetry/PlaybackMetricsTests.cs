using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Telemetry;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Telemetry;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Controller.Tests.Telemetry;

public class PlaybackMetricsTests
{
    [Fact]
    public void PlaybackBitrate_IsReportedPerClient_AndDropsToZeroOnStop()
    {
        const string Client = "PlaybackMetricsTests";
        const string PlaySessionId = "playback-metrics-tests-session";

        var started = Collect(() => PlaybackMetrics.OnPlaybackStarted(
            PlaySessionId,
            null,
            PlayMethod.DirectPlay,
            MediaType.Video,
            Client,
            4_000_000));

        Assert.Equal(4_000_000, Value(started, "jellyfin.playback.bitrate", Client));
        Assert.Equal(1, Value(started, "jellyfin.playback.sessions.active", Client));
        Assert.Equal(1, Value(started, "jellyfin.playback.started", Client));

        var stopped = Collect(() => PlaybackMetrics.OnPlaybackStopped(PlaySessionId, null, true, false));

        // Both gauges keep reporting the combination, at zero, so the series does not go stale.
        Assert.Equal(0, Value(stopped, "jellyfin.playback.bitrate", Client));
        Assert.Equal(0, Value(stopped, "jellyfin.playback.sessions.active", Client));
        Assert.Equal(1, Value(stopped, "jellyfin.playback.stopped", Client));
    }

    [Fact]
    public void TranscodeProgress_RecordsThroughputRelativeToTheSource()
    {
        const string JobId = "playback-metrics-tests-job";

        PlaybackMetrics.OnTranscodeStarted(
            JobId,
            TranscodingJobType.Hls,
            HardwareAccelerationType.qsv,
            "h264",
            "aac",
            TranscodeReason.ContainerNotSupported);

        // Half of the source framerate, so the job produces one second of video every two seconds.
        var progress = Collect(() => PlaybackMetrics.OnTranscodeProgress(JobId, 12f, 3_000_000, 24f));

        Assert.Equal(12, Value(progress, "jellyfin.transcode.framerate", null));
        Assert.Equal(0.5, Value(progress, "jellyfin.transcode.speed", null));
        Assert.Equal(3_000_000, Value(progress, "jellyfin.transcode.bitrate", null));

        var stopped = Collect(() => PlaybackMetrics.OnTranscodeStopped(JobId));

        Assert.Equal(0, Value(stopped, "jellyfin.transcode.bitrate", null));
    }

    /// <summary>
    /// Runs <paramref name="act"/> and returns everything the Jellyfin meter published while it ran,
    /// including a reading of every observable instrument taken afterwards.
    /// </summary>
    private static List<Measured> Collect(Action act)
    {
        var measurements = new List<Measured>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (ReferenceEquals(instrument.Meter, JellyfinTelemetry.Meter))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.Start();

        act();
        listener.RecordObservableInstruments();

        return measurements;
    }

    private static void Record(List<Measured> measurements, Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        string? client = null;
        foreach (var tag in tags)
        {
            if (string.Equals(tag.Key, "jellyfin.client", StringComparison.Ordinal))
            {
                client = tag.Value as string;
            }
        }

        measurements.Add(new Measured(instrument.Name, value, client));
    }

    private static double Value(List<Measured> measurements, string instrument, string? client)
        => Assert.Single(measurements, m => string.Equals(m.Instrument, instrument, StringComparison.Ordinal) && m.Client == client).Value;

    private sealed record Measured(string Instrument, double Value, string? Client);
}
