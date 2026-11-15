using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

/// <summary>
/// What the stress test reads: only single-input local files loop, a storage outage or unreadable source is a failure
/// rather than a capacity limit, and sources too short to measure are refused.
/// </summary>
public sealed class TranscodeTestInputTests : IDisposable
{
    private const string SingleInput = "-analyzeduration 200M -probesize 1G -f matroska -init_hw_device videotoolbox=vt -hwaccel videotoolbox -hwaccel_output_format videotoolbox_vld -noautorotate -i file:\"/media/Movie.mkv\" -map_metadata -1 -map_chapters -1 -threads 0 -map 0:0 -map 0:1 -map -0:s -codec:v:0 h264_videotoolbox -codec:a:0 aac -f hls \"/transcodes/out.m3u8\"";
    private const string ExternalAudio = "-analyzeduration 200M -probesize 1G -f matroska -noautorotate -i file:\"/media/ExtAudio.mkv\" -i file:\"/media/ExtAudio.eng.m4a\" -map_metadata -1 -map_chapters -1 -threads 0 -map 0:0 -map 1:0 -map -0:s -codec:v:0 h264_videotoolbox -codec:a:0 aac -f hls \"/transcodes/out.m3u8\"";
    private const string ExternalSubtitle = "-analyzeduration 200M -probesize 1G -f matroska -noautorotate -i file:\"/media/Subs.mkv\" -i file:\"/media/Subs.en.ass\" -map_metadata -1 -map_chapters -1 -threads 0 -map 0:0 -map 0:1 -codec:v:0 h264_videotoolbox -vf \"subtitles=f='/media/Subs.en.ass'\" -codec:a:0 aac -f hls \"/transcodes/out.m3u8\"";

    private static readonly MediaSourceInfo _localFile = new() { Protocol = MediaProtocol.File };

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "transcode-test-input-" + Guid.NewGuid().ToString("N"));

    public TranscodeTestInputTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, true);

    [Theory]
    [InlineData(SingleInput, 1)]
    [InlineData(ExternalAudio, 2)]
    [InlineData(ExternalSubtitle, 2)]
    [InlineData("-i file:\"/media/My -i Movie.mkv\" -f hls out.m3u8", 1)]
    [InlineData("-i file:\"/media/A \\\" -i B.mkv\" -i file:\"/media/B.m4a\" -f hls out.m3u8", 2)]
    [InlineData("-f hls out.m3u8", 0)]
    [InlineData("-i\tfile:\"/a.mkv\"\n-i file:\"/b.m4a\"", 2)]
    [InlineData("-init_hw_device vt -ignore_unknown -i_qfactor 1 -i \"a.mkv\"", 1)]
    public void CountInputs_CountsOnlyInputOptionsOutsideQuotedPaths(string arguments, int expected)
        => Assert.Equal(expected, TranscodeTestManager.CountInputs(arguments));

    [Fact]
    public void LoopsInput_SingleLocalInputLoops()
    {
        Assert.True(TranscodeTestManager.LoopsInput(_localFile, VideoType.VideoFile, 10_000_000, SingleInput));
        var arguments = TranscodeTestManager.StressArguments(SingleInput, true);
        Assert.StartsWith(TranscodeTestManager.StressLogArguments + "-stream_loop -1 -analyzeduration", arguments, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ExternalAudio)]
    [InlineData(ExternalSubtitle)]
    public void LoopsInput_ExternalAudioOrSubtitleInputsUseTheRestartPath(string playback)
    {
        // -stream_loop would loop only the first input: the external file would end after one pass.
        var loop = TranscodeTestManager.LoopsInput(_localFile, VideoType.VideoFile, 10_000_000, playback);
        Assert.False(loop);
        var arguments = TranscodeTestManager.StressArguments(playback, loop);
        Assert.DoesNotContain("-stream_loop", arguments, StringComparison.Ordinal);
        Assert.Equal(TranscodeTestManager.StressLogArguments + playback, arguments);
    }

    [Fact]
    public void LoopsInput_RemoteSingleInputStillDoesNotLoop()
        => Assert.False(TranscodeTestManager.LoopsInput(new MediaSourceInfo { Protocol = MediaProtocol.Http }, VideoType.VideoFile, 10_000_000, "-i \"http://127.0.0.1/a.mkv\" -f hls out.m3u8"));

    [Theory]
    [InlineData("[in#0/matroska,webm @ 0x9d] Read error at pos. 190103455 (0xb54c99f)")]
    [InlineData("[in#0/matroska,webm @ 0x9d] Error during demuxing: Input/output error")]
    [InlineData("[in#0/mov,mp4,m4a,3gp,3g2,mj2 @ 0x1] Error during demuxing: Stale file handle")]
    [InlineData("[in#0/matroska,webm @ 0x1] Error during demuxing: Invalid data found when processing input")]
    [InlineData("[in#0 @ 0x1] Error opening input: No such file or directory")]
    [InlineData("Error opening input files: Input/output error")]
    [InlineData("[matroska,webm @ 0x1] Seek to desired resync point failed: Input/output error")]
    public void IsInputFailureLine_ReadErrorsAreInputFailures(string line)
        => Assert.True(TranscodeTestRules.IsInputFailureLine(line));

    [Theory]
    [InlineData("[vist#0:0/hevc @ 0x1] [dec:hevc @ 0x2] Decoder returned EOF, resetting")]
    [InlineData("[in#0/matroska,webm @ 0x75d034000] EOF while reading input")]
    [InlineData("[in#0/matroska,webm @ 0x1] Error during demuxing: End of file")]
    [InlineData("[in#0/matroska,webm @ 0x1] Input stream #0:0 (video): 14 packets read (2673520 bytes); 2 frames decoded; 0 decode errors;")]
    [InlineData("[AVIOContext @ 0x1] Statistics: 1503615115 bytes read, 8 seeks")]
    [InlineData("Input #0, matroska,webm, from 'file:/Volumes/Read error at pos. 5/Error during demuxing: x.mkv':")]
    [InlineData("title           : Error during demuxing: Input/output error")]
    [InlineData("[in#0 @ 0x1] Opening an input file: /Volumes/Stale file handle/a.mkv.")]
    [InlineData("[error] Cannot allocate memory")]
    public void IsInputFailureLine_NormalOutputPathsAndTitlesAreNot(string line)
        => Assert.False(TranscodeTestRules.IsInputFailureLine(line));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InputFailure_IsStickyAndCannotBecomeARestart(bool looping)
    {
        var stream = Ended(0, looping);
        try
        {
            if (!looping)
            {
                // A clean end of a finite input that is not looped would otherwise start the stream again.
                Assert.Equal(TranscodeTestExitDisposition.Restart, TranscodeTestManager.ExitDisposition(stream));
            }

            stream.OnLine("[in#0/matroska,webm @ 0x1] [error] Read error at pos. 1792 (0x700)", 0.3);
            for (var i = 0; i < 20; i++)
            {
                stream.OnLine("[info] normal output after the error", 0.4 + (i / 100.0));
            }

            Assert.Equal("[in#0/matroska,webm @ 0x1] Read error at pos. 1792 (0x700)", stream.InputFailureLine);
            Assert.Null(stream.PrematureEndLine);
            Assert.Equal(TranscodeTestExitDisposition.Failure, TranscodeTestManager.ExitDisposition(stream));
        }
        finally
        {
            stream.DisposeProcess(false);
        }
    }

    [Fact]
    public void InputFailure_EveryPassOfALoopedInputIsNotAFailure()
    {
        var stream = Ended(0, true);
        try
        {
            // ffmpeg 8.1 prints these on each pass of -stream_loop -1.
            for (var pass = 0; pass < 5; pass++)
            {
                stream.OnLine("[in#0/matroska,webm @ 0x75d034000] [verbose] EOF while reading input", 0.3 + pass);
                stream.OnLine("[vist#0:0/hevc @ 0x1] [dec:hevc @ 0x2] [verbose] Decoder returned EOF, resetting", 0.4 + pass);
                stream.OnLine("[aist#0:1/dts @ 0x3] [dec:dca @ 0x4] [verbose] Decoder returned EOF, resetting", 0.5 + pass);
            }

            Assert.Null(stream.InputFailureLine);
        }
        finally
        {
            stream.DisposeProcess(false);
        }
    }

    [Fact]
    public async Task DescribeUnavailableSourceAsync_ReadableFileOrNoLocalFileIsAvailable()
    {
        var path = Path.Combine(_folder, "movie.mkv");
        await File.WriteAllBytesAsync(path, [1, 2, 3], TestContext.Current.CancellationToken);
        Assert.Null(await TranscodeTestManager.DescribeUnavailableSourceAsync(path, TimeSpan.FromSeconds(5)));
        Assert.Null(await TranscodeTestManager.DescribeUnavailableSourceAsync(null, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task DescribeUnavailableSourceAsync_VanishedFileOrMountIsUnavailable()
    {
        var path = Path.Combine(_folder, "unmounted", "movie.mkv");
        var unavailable = await TranscodeTestManager.DescribeUnavailableSourceAsync(path, TimeSpan.FromSeconds(5));
        Assert.StartsWith("The media file became unavailable: ", unavailable, StringComparison.Ordinal);

        var empty = Path.Combine(_folder, "empty.mkv");
        await File.WriteAllBytesAsync(empty, [], TestContext.Current.CancellationToken);
        Assert.Equal("The media file became unavailable: it is now empty.", await TranscodeTestManager.DescribeUnavailableSourceAsync(empty, TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0L, false)]
    [InlineData(416_667L, true)]
    [InlineData(20_000_000L, true)]
    [InlineData(49_999_999L, true)]
    [InlineData(50_000_000L, false)]
    [InlineData(138_280_000L, false)]
    public void DescribeTooShortSource_RefusesKnownSourcesUnderTheMinimum(long? ticks, bool refused)
    {
        var reason = TranscodeTestRules.DescribeTooShortSource(ticks);
        Assert.Equal(refused, reason is not null);
        if (refused)
        {
            Assert.StartsWith("The stress test needs a video of at least 5 seconds; this one is ", reason, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(20_000_000L, 400)]
    [InlineData(416_667L, 400)]
    public void StartTest_ShortStressSourceIsABadRequest(long ticks, int status)
    {
        var id = Guid.NewGuid();
        var library = new Mock<ILibraryManager>();
        library.Setup(l => l.GetItemById(id)).Returns(new Video { RunTimeTicks = ticks });
        var controller = new TranscodeTestController(null!, library.Object);

        var result = controller.StartTest(new TranscodeTestRequestDto { Kind = TranscodeTestKind.StressTest, ItemId = id });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(status, badRequest.StatusCode);
        Assert.Contains("at least 5 seconds", Assert.IsType<string>(badRequest.Value), StringComparison.Ordinal);
    }

    private static TranscodeTestStream Ended(int exitCode, bool looping)
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
        var stream = new TranscodeTestStream(1, process, string.Empty, string.Empty, 24f, 0, 6)
        {
            SourceSeconds = looping ? null : 0.08
        };
        Assert.True(stream.WaitForExit(TimeSpan.FromSeconds(15)));
        stream.OnLine("frame=2 fps=24 time=00:00:00.08", 0.25);
        return stream;
    }
}
