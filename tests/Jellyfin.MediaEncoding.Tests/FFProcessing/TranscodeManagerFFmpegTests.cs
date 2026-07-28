using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.MediaEncoding.FFProcessing;
using MediaBrowser.MediaEncoding.Transcoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.FFProcessing;

/// <summary>
/// Drives a real FFmpeg through <see cref="TranscodeManager.StartFfMpeg"/>, which is otherwise
/// uncovered. The command line is a parameter of that method, so these exercise the spawn, session
/// and job wiring without involving <c>EncodingHelper</c>'s argument construction.
/// <para>
/// Explicit: needs a real FFmpeg. See "Tests Requiring FFmpeg" in the repository README.
/// </para>
/// </summary>
public sealed class TranscodeManagerFFmpegTests : IClassFixture<SynthesizedMediaFixture>, IDisposable
{
    private readonly SynthesizedMediaFixture _media;
    private readonly string _root;
    private readonly TranscodeManager _manager;

    public TranscodeManagerFFmpegTests(SynthesizedMediaFixture media)
    {
        _media = media;
        _root = Directory.CreateTempSubdirectory("jf-transcode-tests-").FullName;

        var paths = new FFPaths();
        paths.SetEncoderPath(_media.EncoderPath);

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(p => p.LogDirectoryPath).Returns(Path.Combine(_root, "logs"));
        appPaths.SetupGet(p => p.CachePath).Returns(Path.Combine(_root, "cache"));
        Directory.CreateDirectory(Path.Combine(_root, "logs"));

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.ApplicationPaths).Returns(appPaths.Object);
        config.SetupGet(c => c.CommonApplicationPaths).Returns(appPaths.Object);
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration());
        config.Setup(c => c.GetConfiguration("encoding"))
            .Returns(new EncodingOptions { TranscodingTempPath = Path.Combine(_root, "transcodes") });

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.SetupGet(e => e.EncoderPath).Returns(_media.EncoderPath);

        _manager = new TranscodeManager(
            NullLoggerFactory.Instance,
            Mock.Of<IFileSystem>(),
            appPaths.Object,
            config.Object,
            Mock.Of<IUserManager>(),
            Mock.Of<ISessionManager>(),
            new EncodingHelper(
                appPaths.Object,
                mediaEncoder.Object,
                Mock.Of<ISubtitleEncoder>(),
                Mock.Of<IConfiguration>(),
                config.Object,
                Mock.Of<IPathManager>()),
            mediaEncoder.Object,
            new FFRunner(NullLogger<FFRunner>.Instance, paths),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IAttachmentExtractor>());
    }

    public void Dispose()
    {
        _manager.Dispose();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort; the log files may still be held briefly.
        }
    }

    private StreamState NewState() => new(
        Mock.Of<IMediaSourceManager>(),
        TranscodingJobType.Progressive,
        _manager)
    {
        // DeviceId stays null so no session progress is reported.
        Request = new StreamingRequestDto { MediaSourceId = Guid.NewGuid().ToString("N") },
        MediaSource = new MediaSourceInfo { RequiresOpening = false, Path = _media.Mp4 },
        OutputVideoCodec = "copy",
        OutputAudioCodec = "copy"
    };

    /// <summary>
    /// The job's session is released as soon as it ends — <c>OnFfMpegProcessExited</c> disposes the
    /// job — so tests observe the state the rest of the server reads rather than holding the
    /// session.
    /// </summary>
    private static async Task<bool> WaitForExitAsync(TranscodingJob job, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!job.HasExited && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return job.HasExited;
    }

    [Fact(Explicit = true)]
    public async Task StartFfMpeg_ProducesTheOutputAndRecordsACleanExit()
    {
        var state = NewState();
        var output = Path.Combine(_root, "out.mp4");
        using var cts = new CancellationTokenSource();

        var job = await _manager.StartFfMpeg(
            state,
            output,
            $"-f lavfi -i testsrc=s=64x64:d=1 -c:v libx264 \"{output}\"",
            Guid.Empty,
            TranscodingJobType.Progressive,
            cts);

        Assert.True(await WaitForExitAsync(job, TimeSpan.FromSeconds(30)), "ffmpeg did not finish");

        Assert.Equal(0, job.ExitCode);
        Assert.True(File.Exists(output));
        Assert.True(new FileInfo(output).Length > 0);
    }

    [Fact(Explicit = true)]
    public async Task StartFfMpeg_WritesTheCommandLineIntoAJobLog()
    {
        var state = NewState();
        var output = Path.Combine(_root, "logged.mp4");
        using var cts = new CancellationTokenSource();

        var job = await _manager.StartFfMpeg(
            state,
            output,
            $"-f lavfi -i testsrc=s=64x64:d=1 -c:v libx264 \"{output}\"",
            Guid.Empty,
            TranscodingJobType.Progressive,
            cts);

        Assert.True(await WaitForExitAsync(job, TimeSpan.FromSeconds(30)), "ffmpeg did not finish");

        // Remux because both codecs are copy, which decides the log filename.
        var logs = Directory.GetFiles(Path.Combine(_root, "logs"), "FFmpeg.Remux-*.log");
        Assert.NotEmpty(logs);

        var log = await File.ReadAllTextAsync(logs[0], TestContext.Current.CancellationToken);

        // Assert against the header line as a unit rather than the whole file. FFmpeg's own stderr is
        // appended to the same log, so a substring search across all of it could be satisfied by
        // something the encoder printed rather than by the header actually being complete.
        var header = Assert.Single(
            log.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            line => line.Contains("-hide_banner", StringComparison.Ordinal));

        // People paste this line out of bug reports to reproduce a failure, so it has to be the whole
        // command — the caller's arguments *and* every flag the runner supplies. A header missing
        // them sends someone chasing a command that behaves differently from the one that ran.
        Assert.Contains(_media.EncoderPath, header, StringComparison.Ordinal);
        Assert.Contains("testsrc", header, StringComparison.Ordinal);
        Assert.Contains("-loglevel", header, StringComparison.Ordinal);
        Assert.Contains("-stats", header, StringComparison.Ordinal);

        // Specifically the overwrite flag, which the argument builders no longer emit themselves.
        Assert.Contains("-y", header, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task StartFfMpeg_DeliversHlsSegmentsAndAPlaylist()
    {
        var state = NewState();
        var hlsDir = Path.Combine(_root, "hls");
        Directory.CreateDirectory(hlsDir);
        var playlist = Path.Combine(hlsDir, "main.m3u8");

        using var cts = new CancellationTokenSource();

        var job = await _manager.StartFfMpeg(
            state,
            playlist,
            $"-f lavfi -i testsrc=s=64x64:d=4 -c:v libx264 -g 24 -f hls -hls_time 1 "
                + $"-hls_segment_filename \"{Path.Combine(hlsDir, "seg%d.ts")}\" \"{playlist}\"",
            Guid.Empty,
            TranscodingJobType.Hls,
            cts);

        Assert.True(await WaitForExitAsync(job, TimeSpan.FromSeconds(60)), "ffmpeg did not finish");

        Assert.Equal(0, job.ExitCode);
        Assert.True(File.Exists(playlist));
        Assert.NotEmpty(Directory.GetFiles(hlsDir, "seg*.ts"));
        Assert.Contains("#EXTM3U", await File.ReadAllTextAsync(playlist, TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task Throttler_PausesAndResumesARunningTranscodeThroughTheSession()
    {
        var state = NewState();
        var output = Path.Combine(_root, "throttled.mp4");
        using var cts = new CancellationTokenSource();

        var job = await _manager.StartFfMpeg(
            state,
            output,
            $"-f lavfi -i testsrc=s=64x64:d=3600 -c:v libx264 \"{output}\"",
            Guid.Empty,
            TranscodingJobType.Progressive,
            cts);

        Assert.NotNull(job.Session);

        // The throttler steers the process by writing runtime keys to the session, which is the
        // path that replaced writing to Process.StandardInput directly.
        await job.Session!.SendKeyAsync("p", TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.False(job.HasExited);

        await job.Session!.SendKeyAsync("u", TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        Assert.False(job.HasExited);

        job.Stop();
        Assert.True(await WaitForExitAsync(job, TimeSpan.FromSeconds(30)), "the job did not stop");
    }

    [Fact(Explicit = true)]
    public async Task KillTranscodingJob_StopsALongRunningTranscode()
    {
        var state = NewState();
        var output = Path.Combine(_root, "killed.mp4");
        using var cts = new CancellationTokenSource();

        var job = await _manager.StartFfMpeg(
            state,
            output,
            $"-f lavfi -i testsrc=s=64x64:d=3600 -c:v libx264 \"{output}\"",
            Guid.Empty,
            TranscodingJobType.Progressive,
            cts);

        // An hour-long source, so it is certainly still running.
        Assert.False(job.HasExited);
        Assert.NotNull(job.Session);

        job.Stop();

        Assert.True(await WaitForExitAsync(job, TimeSpan.FromSeconds(30)), "the job did not stop");
    }
}
