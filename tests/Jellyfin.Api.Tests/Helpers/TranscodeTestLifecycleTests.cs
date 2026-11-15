using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

/// <summary>
/// The supervisor's launch ownership, same-tick exit priority and finite local input continuity.
/// </summary>
public sealed class TranscodeTestLifecycleTests
{
    [Theory]
    [InlineData(true, 0, 0, false, false)]
    [InlineData(false, 1, 0, false, false)]
    [InlineData(false, 0, 1, false, false)]
    [InlineData(false, 0, 0, true, false)]
    [InlineData(false, 0, 0, false, true)]
    public void CanAddStressStream_DoesNotOverwriteAPendingLaunchOrJudgeReducedLoad(bool pending, int queued, int draining, bool cap, bool expected)
        => Assert.Equal(expected, TranscodeTestManager.CanAddStressStream(pending, queued, draining, cap));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SameTickExitBatch_ActualPeerFailurePrecedesTheRestartCap(bool capFirst)
    {
        var capped = Ended(1, 0, 100, 0.1);
        var failed = Ended(2, 7, 0, 120);
        try
        {
            var ordered = TranscodeTestManager.OrderExitedStreams(capFirst ? [capped, failed] : [failed, capped]);

            Assert.Same(failed, ordered[0]);
            Assert.Equal(TranscodeTestExitDisposition.Failure, TranscodeTestManager.ExitDisposition(ordered[0]));
            Assert.Equal(TranscodeTestExitDisposition.RestartLimit, TranscodeTestManager.ExitDisposition(ordered[1]));
        }
        finally
        {
            capped.DisposeProcess(false);
            failed.DisposeProcess(false);
        }
    }

    [Fact]
    public void RestartCap_AnUnmeasuredInputIsNotAPerformanceResult()
    {
        var capped = Ended(1, 0, 100, 0.1);
        try
        {
            Assert.Null(capped.RunRate);
            Assert.Equal(TranscodeTestExitDisposition.RestartLimit, TranscodeTestManager.ExitDisposition(capped));
            Assert.Contains("without measuring capacity", TranscodeTestRules.DescribeRestartLimit(1, 100, 0), StringComparison.Ordinal);
        }
        finally
        {
            capped.DisposeProcess(false);
        }
    }

    [Fact]
    public void IncompleteDiagnostics_CannotAuthorizeHealthyEofOrRestartCap()
    {
        var capped = Ended(1, 0, 100, 0.1);
        var reader = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            capped.StartReading(() => reader.Task);

            Assert.Equal(TranscodeTestExitDisposition.Failure, TranscodeTestManager.ExitDisposition(capped));
        }
        finally
        {
            reader.TrySetResult();
            capped.DisposeProcess(false);
        }
    }

    [Fact]
    public async Task FaultedOutputReader_CannotAuthorizeHealthyEofOrRestartCap()
    {
        var capped = Ended(1, 0, 100, 0.1);
        try
        {
            capped.StartReading(() => Task.FromException(new IOException("The stderr pipe failed.")));
            await Assert.ThrowsAsync<IOException>(() => capped.WaitForOutputAsync(TimeSpan.FromSeconds(5)));

            Assert.True(capped.OutputCompleted);
            Assert.False(capped.OutputSucceeded);
            Assert.Equal(TranscodeTestExitDisposition.Failure, TranscodeTestManager.ExitDisposition(capped));
        }
        finally
        {
            capped.DisposeProcess(false);
        }
    }

    [Fact]
    public async Task UnexpectedOutputReadFailure_IsNotSuccessfulCompletion()
    {
        using var process = new Process();
        var stream = new TranscodeTestStream(1, process, string.Empty, string.Empty, 24f, 0, 6);

        // StandardError is unavailable on an unstarted process: exercise the production exception path.
        await stream.ReadProgressAsync(Stopwatch.StartNew());

        Assert.True(stream.OutputCompleted);
        Assert.False(stream.OutputSucceeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StressArguments_InputLoopPrecedesInputAndLeavesPlaybackEncodingUntouched(bool loop)
    {
        const string playback = "-analyzeduration 200M -probesize 1G -i \"source.mkv\" -map 0:0 -c:v h264_qsv -vf scale_qsv=w=1920 -c:a copy -f hls \"out.m3u8\"";
        var arguments = TranscodeTestManager.StressArguments(playback, loop);

        Assert.Equal(TranscodeTestManager.StressLogArguments + (loop ? "-stream_loop -1 " : string.Empty) + playback, arguments);
        Assert.EndsWith(playback, arguments, StringComparison.Ordinal);
        if (loop)
        {
            Assert.True(arguments.IndexOf("-stream_loop", StringComparison.Ordinal) < arguments.IndexOf("-i ", StringComparison.Ordinal));
        }
        else
        {
            Assert.DoesNotContain("-stream_loop", arguments, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(MediaProtocol.File, VideoType.VideoFile, 10_000_000L, false, true)]
    [InlineData(MediaProtocol.File, VideoType.VideoFile, null, false, false)]
    [InlineData(MediaProtocol.File, VideoType.VideoFile, 0L, false, false)]
    [InlineData(MediaProtocol.File, VideoType.VideoFile, -1L, false, false)]
    [InlineData(MediaProtocol.Http, VideoType.VideoFile, 10_000_000L, false, false)]
    [InlineData(MediaProtocol.File, VideoType.Dvd, 10_000_000L, false, false)]
    [InlineData(MediaProtocol.File, VideoType.VideoFile, 10_000_000L, true, false)]
    public void CanLoopInput_OnlyFiniteLocalVideoFiles(MediaProtocol protocol, VideoType type, long? duration, bool infinite, bool expected)
        => Assert.Equal(expected, TranscodeTestManager.CanLoopInput(new MediaSourceInfo { Protocol = protocol, IsInfiniteStream = infinite }, type, duration));

    [Fact]
    public void CanLoopInput_EncoderProtocolAndLiveSourceCanVetoFileMetadata()
    {
        Assert.False(TranscodeTestManager.CanLoopInput(new MediaSourceInfo { Protocol = MediaProtocol.File, EncoderProtocol = MediaProtocol.Http }, VideoType.VideoFile, 10_000_000));
        Assert.False(TranscodeTestManager.CanLoopInput(new MediaSourceInfo { Protocol = MediaProtocol.File, LiveStreamId = "active-live-source" }, VideoType.VideoFile, 10_000_000));
        Assert.False(TranscodeTestManager.CanLoopInput(new MediaSourceInfo { Protocol = MediaProtocol.File, RequiresLooping = true }, VideoType.VideoFile, 10_000_000));
    }

    private static TranscodeTestStream Ended(int id, int exitCode, int restarts, double sourceSeconds)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(
                OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                OperatingSystem.IsWindows() ? "/c exit " + exitCode : "-c \"exit " + exitCode + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var stream = new TranscodeTestStream(id, process, string.Empty, string.Empty, 24f, 0, 6)
        {
            SourceSeconds = sourceSeconds,
            Restarts = restarts
        };
        Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));
        stream.OnLine("frame=2 fps=24 time=00:00:00.08", 0.25);
        return stream;
    }
}
