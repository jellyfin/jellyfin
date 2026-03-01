using System;
using System.Threading;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.ClipDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class ClipFfmpegRunnerTests
{
    private static ClipJob MakeJob() => new ClipJob
    {
        ClipId = "test",
        ItemId = Guid.NewGuid(),
        OutputPath = "/tmp/test.mp4",
        DurationTicks = 30_000_000_000L, // 30 seconds
        StartTimeTicks = 0,
        EndTimeTicks = 30_000_000_000L,
        CancellationTokenSource = new CancellationTokenSource()
    };

    // ── ParseLine — normal cases ──────────────────────────────────────

    [Fact]
    public void ParseLine_HalfwayThrough_SetsApproximately50Percent()
    {
        var job = MakeJob();
        ClipFfmpegRunner.ParseLine("frame=  10 fps=30 time=00:00:15.00 bitrate= 100kbits/s", job, 30.0);
        Assert.InRange(job.ProgressPercent, 49.0, 51.0);
    }

    [Fact]
    public void ParseLine_NoTimestamp_DoesNotUpdateProgress()
    {
        var job = MakeJob();
        ClipFfmpegRunner.ParseLine("ffmpeg version 6.0", job, 30.0);
        Assert.Equal(0.0, job.ProgressPercent);
    }

    [Fact]
    public void ParseLine_ZeroDuration_DoesNotDivideByZero()
    {
        var job = MakeJob();

        // Should not throw
        ClipFfmpegRunner.ParseLine("frame=  10 fps=30 time=00:00:05.00 bitrate= 100kbits/s", job, 0.0);
        Assert.Equal(0.0, job.ProgressPercent);
    }

    [Fact]
    public void ParseLine_ProgressCappedAt99Point9()
    {
        var job = MakeJob();

        // time > duration — should cap at 99.9
        ClipFfmpegRunner.ParseLine("frame=  10 fps=30 time=00:01:00.00 bitrate= 100kbits/s", job, 30.0);
        Assert.Equal(99.9, job.ProgressPercent);
    }

    // ── ParseLine — bounds check (regression for truncated lines) ─────

    [Fact]
    public void ParseLine_TruncatedAfterTimeEquals_DoesNotThrow()
    {
        var job = MakeJob();

        // "time=" at the very end — no room for 11 chars
        ClipFfmpegRunner.ParseLine("frame= 10 time=", job, 30.0);
        Assert.Equal(0.0, job.ProgressPercent);
    }

    [Fact]
    public void ParseLine_TruncatedPartialTimestamp_DoesNotThrow()
    {
        var job = MakeJob();

        // Only 5 chars after "time=" instead of 11
        ClipFfmpegRunner.ParseLine("frame= 10 time=00:00", job, 30.0);
        Assert.Equal(0.0, job.ProgressPercent);
    }
}
