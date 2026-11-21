using Jellyfin.Data.Enums;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Telemetry;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Xunit;

namespace Jellyfin.Controller.Tests.Telemetry;

public class PlaybackMetricsTests
{
    private const string ClientTag = "jellyfin.client";
    private const string HardwareAccelerationTag = "jellyfin.transcode.hardware_acceleration";

    [Fact]
    public void PlaybackBitrate_IsReportedPerClient_AndDropsToZeroOnStop()
    {
        const string Client = "PlaybackMetricsTests";
        const string PlaySessionId = "playback-metrics-tests-session";

        var started = MeterCollector.Collect(() => PlaybackMetrics.OnPlaybackStarted(
            PlaySessionId,
            null,
            PlayMethod.DirectPlay,
            MediaType.Video,
            Client,
            4_000_000));

        Assert.Equal(4_000_000, MeterCollector.Value(started, "jellyfin.playback.bitrate", (ClientTag, Client)));
        Assert.Equal(1, MeterCollector.Value(started, "jellyfin.playback.sessions.active", (ClientTag, Client)));
        Assert.Equal(1, MeterCollector.Value(started, "jellyfin.playback.started", (ClientTag, Client)));

        var stopped = MeterCollector.Collect(() => PlaybackMetrics.OnPlaybackStopped(PlaySessionId, null, true, false));

        // Both gauges keep reporting the combination, at zero, so the series does not go stale.
        Assert.Equal(0, MeterCollector.Value(stopped, "jellyfin.playback.bitrate", (ClientTag, Client)));
        Assert.Equal(0, MeterCollector.Value(stopped, "jellyfin.playback.sessions.active", (ClientTag, Client)));
        Assert.Equal(1, MeterCollector.Value(stopped, "jellyfin.playback.stopped", (ClientTag, Client)));
    }

    [Fact]
    public void PlaybackStoppedWithoutPlaySessionId_EndsTheSessionsPlayback()
    {
        const string Client = "PlaybackMetricsTests.Idle";
        const string SessionId = "playback-metrics-tests-idle-session";

        PlaybackMetrics.OnPlaybackStarted("playback-metrics-tests-idle-play-session", SessionId, PlayMethod.Transcode, MediaType.Video, Client, 2_000_000);

        // The idle check stops playback with only the session id.
        var stopped = MeterCollector.Collect(() => PlaybackMetrics.OnPlaybackStopped(null, SessionId, false, false));

        Assert.Equal(0, MeterCollector.Value(stopped, "jellyfin.playback.sessions.active", (ClientTag, Client)));
        Assert.Equal(1, MeterCollector.Value(stopped, "jellyfin.playback.stopped", (ClientTag, Client)));
    }

    [Fact]
    public void SessionEndedWhilePlaying_EndsItsPlayback()
    {
        const string Client = "PlaybackMetricsTests.Ended";
        const string SessionId = "playback-metrics-tests-ended-session";

        PlaybackMetrics.OnPlaybackStarted(null, SessionId, PlayMethod.DirectPlay, MediaType.Audio, Client, 320_000);

        var ended = MeterCollector.Collect(() => PlaybackMetrics.OnSessionEnded(SessionId));

        Assert.Equal(0, MeterCollector.Value(ended, "jellyfin.playback.sessions.active", (ClientTag, Client)));
        Assert.Equal(0, MeterCollector.Value(ended, "jellyfin.playback.bitrate", (ClientTag, Client)));
    }

    [Fact]
    public void TranscodeProgress_RecordsThroughputRelativeToTheSource()
    {
        const string JobId = "playback-metrics-tests-job";
        const string HardwareAcceleration = "qsv";

        PlaybackMetrics.OnTranscodeStarted(
            JobId,
            TranscodingJobType.Hls,
            HardwareAccelerationType.qsv,
            "h264",
            "aac",
            TranscodeReason.ContainerNotSupported);

        // The job produces one second of media every two seconds.
        var progress = MeterCollector.Collect(() => PlaybackMetrics.OnTranscodeProgress(JobId, 12f, 3_000_000, 0.5));

        Assert.Equal(12, MeterCollector.Value(progress, "jellyfin.transcode.framerate", (HardwareAccelerationTag, HardwareAcceleration)));
        Assert.Equal(0.5, MeterCollector.Value(progress, PlaybackMetrics.TranscodeSpeedName, (HardwareAccelerationTag, HardwareAcceleration)));
        Assert.Equal(3_000_000, MeterCollector.Value(progress, "jellyfin.transcode.bitrate", (HardwareAccelerationTag, HardwareAcceleration)));

        var stopped = MeterCollector.Collect(() => PlaybackMetrics.OnTranscodeStopped(JobId));

        Assert.Equal(0, MeterCollector.Value(stopped, "jellyfin.transcode.bitrate", (HardwareAccelerationTag, HardwareAcceleration)));
    }
}
