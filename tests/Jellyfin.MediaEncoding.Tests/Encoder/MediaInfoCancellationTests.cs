using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Encoder;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Encoder;

public sealed class MediaInfoCancellationTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "jellyfin-probe-cancellation-" + Guid.NewGuid().ToString("N"));

    public MediaInfoCancellationTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public async Task GetMediaInfo_CancelledWhileCopyingOutput_StopsTheProbe()
    {
        using var encoder = CreateEncoder();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var probe = ProbeAsync(encoder, "cancel", cancellation.Token);
        using var process = await StartedProbeAsync(probe);
        try
        {
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probe.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(process.HasExited);
        }
        finally
        {
            StopFixture(process);
        }
    }

    [Fact]
    public Task GetMediaInfo_InvalidJsonResult_ThrowsTheProbeFailure()
        => AssertInvalidProbeAsync(null);

    [Fact]
    public async Task GetMediaInfo_InvalidResultWithAStillRunningProbe_StopsTheProbe()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "A still-running probe closing stdout uses a Unix shell fixture; cancellation and invalid JSON are tested on every OS.");
        await AssertInvalidProbeAsync(await CreateUnixProbeAsync());
    }

    private async Task AssertInvalidProbeAsync(string? probePath)
    {
        using var encoder = CreateEncoder(probePath);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var probe = ProbeAsync(encoder, "invalid", cancellation.Token);
        using var process = await StartedProbeAsync(probe);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(_folder, "release"), "continue", TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<FfmpegException>(() => probe.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(process.HasExited);
        }
        finally
        {
            await cancellation.CancelAsync();
            StopFixture(process);
        }
    }

    private static MediaEncoder CreateEncoder(string? probePath = null)
    {
        var encoder = new MediaEncoder(
            NullLogger<MediaEncoder>.Instance,
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IServerConfigurationManager>());
        // Inject only the executable path, avoiding startup capability discovery. The operation under test is
        // the public GetMediaInfo API, not a reflected private method.
        var executable = OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe";
        typeof(MediaEncoder).GetField("_ffprobePath", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(
            encoder,
            probePath ?? Path.Combine(AppContext.BaseDirectory, "FakeProbe", executable));
        return encoder;
    }

    private async Task<string> CreateUnixProbeAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException();
        }

        var path = Path.Combine(_folder, "ffprobe");
        await File.WriteAllTextAsync(
            path,
            """
            #!/bin/sh
            input="${2#file:}"
            folder="${input%/*}"
            echo $$ > "$folder/pid"
            while [ ! -f "$folder/release" ]; do sleep 0.02; done
            printf '{}'
            exec 1>&-
            exec sleep 60

            """,
            TestContext.Current.CancellationToken);
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }

    private Task<MediaInfo> ProbeAsync(MediaEncoder encoder, string behavior, CancellationToken token)
        => encoder.GetMediaInfo(
            new MediaInfoRequest
            {
                MediaSource = new MediaSourceInfo
                {
                    Path = Path.Combine(_folder, behavior),
                    Protocol = MediaProtocol.File,
                    VideoType = VideoType.VideoFile
                },
                MediaType = DlnaProfileType.Video
            },
            token);

    private async Task<Process> StartedProbeAsync(Task probe)
    {
        var path = Path.Combine(_folder, "pid");
        var wait = Stopwatch.StartNew();
        while (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            if (probe.IsCompleted)
            {
                await probe;
            }

            Assert.True(wait.Elapsed < TimeSpan.FromSeconds(10), "The fake probe did not start.");
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        var pid = int.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
        return Process.GetProcessById(pid);
    }

    private static void StopFixture(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(true);
            process.WaitForExit(TimeSpan.FromSeconds(5));
        }
    }
}
