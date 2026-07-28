using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using MediaBrowser.MediaEncoding.FFProcessing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.FFProcessing;

/// <summary>
/// Covers the start-and-hold contract that live recording and delivery streaming both rely on.
/// Every test is <c>Explicit</c> because it needs a real FFmpeg; see "Tests Requiring FFmpeg" in
/// the repository README.
/// </summary>
public sealed class FFSessionFFmpegTests : IClassFixture<SynthesizedMediaFixture>, IDisposable
{
    private readonly FFRunner _runner;

    public FFSessionFFmpegTests(SynthesizedMediaFixture media)
    {
        var paths = new FFPaths();
        paths.SetEncoderPath(media.EncoderPath);
        _runner = new FFRunner(NullLogger<FFRunner>.Instance, paths);
    }

    /// <summary>A stream that would run for an hour, so it is certainly still going.</summary>
    private static StreamRequest LongRunning(FFOutputSink? stderr = null) => new()
    {
        Delivery = FFDelivery.Progressive,
        Mode = StreamMode.Transcode,
        Arguments = "-f lavfi -i testsrc=s=64x64:d=3600 -f null -",
        Stderr = stderr ?? FFOutputSink.Diagnostic()
    };

    public void Dispose() => _runner.Dispose();

    [Fact(Explicit = true)]
    public async Task StartAsync_ReturnsWhileTheProcessIsStillRunning()
    {
        await using var session = await _runner.StartAsync(LongRunning(), CancellationToken.None);

        Assert.False(session.Completion.IsCompleted);
        Assert.True(session.ProcessId > 0);
    }

    [Fact(Explicit = true)]
    public async Task StopAsync_EndsItAndReportsCancellationRatherThanFailure()
    {
        var session = await _runner.StartAsync(LongRunning(), CancellationToken.None);

        var result = await session.StopAsync(CancellationToken.None);

        Assert.Equal(FFStopReason.Cancelled, result.StopReason);

        // A deliberate stop is not a failed run, and must not be reported as one.
        Assert.False(result.Succeeded);
        Assert.True(result.Elapsed > TimeSpan.Zero);
    }

    [Fact(Explicit = true)]
    public async Task CancellingTheToken_StopsTheProcess()
    {
        using var cts = new CancellationTokenSource();
        var session = await _runner.StartAsync(LongRunning(), cts.Token);

        await cts.CancelAsync();
        var result = await session.Completion;

        Assert.Equal(FFStopReason.Cancelled, result.StopReason);
    }

    [Fact(Explicit = true)]
    public async Task Completion_CarriesTheOutcomeOfAShortRun()
    {
        var request = new StreamRequest
        {
            Delivery = FFDelivery.Progressive,
            Mode = StreamMode.Remux,
            Arguments = "-f lavfi -i testsrc=s=64x64:d=1 -f null -"
        };

        await using var session = await _runner.StartAsync(request, CancellationToken.None);
        var result = await session.Completion;

        Assert.True(result.Succeeded, result.Stderr);
        Assert.Equal(FFStopReason.Exited, result.StopReason);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact(Explicit = true)]
    public async Task Completion_ReportsAFailureWithItsStderr()
    {
        var request = new StreamRequest
        {
            Delivery = FFDelivery.Progressive,
            Mode = StreamMode.Transcode,
            Arguments = "-f lavfi -i testsrc=s=64x64:d=1 -vf nosuchfilter -f null -"
        };

        await using var session = await _runner.StartAsync(request, CancellationToken.None);
        var result = await session.Completion;

        Assert.False(result.Succeeded);
        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEmpty(result.Stderr);
    }

    [Fact(Explicit = true)]
    public async Task ToStreamSink_IsReadableBeforeTheProcessExits()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "jf-session-" + Guid.NewGuid().ToString("N") + ".log");

        try
        {
            await using (var log = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                // -loglevel is derived from the logger, so ask FFmpeg for output explicitly.
                var request = LongRunning(FFOutputSink.ToStream(log)) with
                {
                    Arguments = "-v info -f lavfi -i testsrc=s=64x64:d=3600 -f null -"
                };

                var session = await _runner.StartAsync(request, CancellationToken.None);

                // The whole point of this sink: readable while the process is still running.
                var deadline = DateTime.UtcNow.AddSeconds(10);
                long written = 0;
                while (written == 0 && DateTime.UtcNow < deadline)
                {
                    using var reader = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    written = reader.Length;
                    if (written == 0)
                    {
                        await Task.Delay(200, TestContext.Current.CancellationToken);
                    }
                }

                Assert.True(written > 0, "nothing was written to the log while the process was running");
                await session.StopAsync(CancellationToken.None);
            }
        }
        finally
        {
            File.Delete(logPath);
        }
    }

    [Fact(Explicit = true)]
    public async Task StreamAction_KeepsProgressStatsAtAQuietLogLevel()
    {
        // FFmpeg suppresses its status line below info, and the level here is derived from a
        // NullLogger, so this only passes because the Stream policy asks for -stats. Progress
        // reporting and throttling both parse that line.
        var request = new StreamRequest
        {
            Delivery = FFDelivery.Progressive,
            Mode = StreamMode.Transcode,
            Arguments = "-f lavfi -i testsrc=s=64x64:d=2 -f null -"
        };

        await using var session = await _runner.StartAsync(request, CancellationToken.None);
        var result = await session.Completion;

        Assert.True(result.Succeeded, result.Stderr);
        Assert.Contains("time=", result.Stderr, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task NonStreamAction_StaysQuietWithoutTheStatusLine()
    {
        var request = new CapabilitiesRequest { Arguments = "-hwaccels" };

        await using var session = await _runner.StartAsync(request, CancellationToken.None);
        var result = await session.Completion;

        Assert.DoesNotContain("time=", result.Stderr, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task SendKeyAsync_RefusesAnActionThatIsNotSteerable()
    {
        var request = new CapabilitiesRequest { Arguments = "-hwaccels" };
        await using var session = await _runner.StartAsync(request, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.SendKeyAsync("p", CancellationToken.None));
    }
}
