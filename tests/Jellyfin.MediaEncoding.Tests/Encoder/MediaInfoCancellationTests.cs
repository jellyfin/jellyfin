using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.MediaEncoding.Encoder;
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
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake probe fixture uses a Unix shell executable.");
        using var encoder = CreateEncoder();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var probe = ProbeAsync(encoder, "echo $$ > \"$1\"; exec sleep 60", cancellation.Token);
        using var process = await StartedProbeAsync();
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
    public async Task GetMediaInfo_InvalidResultWithAStillRunningProbe_StopsTheProbe()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "The fake probe fixture uses a Unix shell executable.");
        using var encoder = CreateEncoder();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var probe = ProbeAsync(encoder, "echo $$ > \"$1\"; while [ ! -f \"$2\" ]; do sleep 0.02; done; printf '{}'; exec 1>&-; exec sleep 60", cancellation.Token);
        using var process = await StartedProbeAsync();
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

    private static MediaEncoder CreateEncoder()
    {
        var encoder = new MediaEncoder(
            NullLogger<MediaEncoder>.Instance,
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<IFileSystem>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IServerConfigurationManager>());
        typeof(MediaEncoder).GetField("_ffprobePath", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(encoder, "/bin/sh");
        return encoder;
    }

    private Task<MediaInfo> ProbeAsync(MediaEncoder encoder, string script, CancellationToken token)
    {
        var probeArguments = "-c \"" + script.Replace("\"", "\\\"", StringComparison.Ordinal)
            + "\" probe \"" + Path.Combine(_folder, "pid") + "\" \"" + Path.Combine(_folder, "release") + "\"";
        return (Task<MediaInfo>)typeof(MediaEncoder).GetMethod("GetMediaInfoInternal", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
            encoder,
            [string.Empty, string.Empty, MediaProtocol.File, false, probeArguments, false, VideoType.VideoFile, token])!;
    }

    private async Task<Process> StartedProbeAsync()
    {
        var path = Path.Combine(_folder, "pid");
        var wait = Stopwatch.StartNew();
        while (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
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
