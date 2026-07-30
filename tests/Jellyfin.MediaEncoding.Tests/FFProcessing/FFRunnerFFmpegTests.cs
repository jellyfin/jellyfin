using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using MediaBrowser.MediaEncoding.FFProcessing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.FFProcessing;

/// <summary>
/// Runs the rendered command lines through a real FFmpeg, which is the only way to check that the
/// arguments are actually accepted rather than merely well-formed.
/// <para>
/// Every test here is <c>Explicit</c>: it needs an external binary, so leaving it on by default
/// would break contributor machines that lack it, and skipping when the binary is absent would
/// report green for a suite that never ran. Opt in with:
/// </para>
/// <code>dotnet run --project tests/Jellyfin.MediaEncoding.Tests -- -explicit only</code>
/// <para>See "Tests Requiring FFmpeg" in the repository README.</para>
/// </summary>
public sealed class FFRunnerFFmpegTests : IClassFixture<SynthesizedMediaFixture>, IDisposable
{
    private readonly SynthesizedMediaFixture _media;
    private readonly FFRunner _runner;

    public FFRunnerFFmpegTests(SynthesizedMediaFixture media)
    {
        _media = media;

        // The real holder, so these tests also cover the ffmpeg -> ffprobe path derivation.
        var paths = new FFPaths();
        paths.SetEncoderPath(_media.EncoderPath);

        _runner = new FFRunner(NullLogger<FFRunner>.Instance, paths);
    }

    /// <inheritdoc />
    public void Dispose() => _runner.Dispose();

    [Fact(Explicit = true)]
    public async Task Attachment_DumpsFromVideoContainer()
    {
        var target = Path.Combine(_media.Root, "from_av.ttf");

        var result = await RunDumpAsync(_media.VideoWithAttachment, 2, target);

        Assert.True(result.Succeeded, result.Stderr);
        Assert.True(File.Exists(target));
    }

    [Fact(Explicit = true)]
    public async Task Attachment_DumpsFromSubtitleOnlyContainer()
    {
        // The case that previously produced exit 234, "Output file does not contain any stream".
        var target = Path.Combine(_media.Root, "from_mks.ttf");

        var result = await RunDumpAsync(_media.SubtitlesWithAttachment, 1, target);

        Assert.True(result.Succeeded, result.Stderr);
        Assert.True(File.Exists(target));
    }

    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Attachment_SourceWithoutAttachmentsStillSucceeds(bool useMp4)
    {
        // -map 0:t? tolerates matching nothing
        var source = useMp4 ? _media.Mp4 : _media.VideoWithoutAttachment;

        var result = await _runner.RunAsync(
            new AttachmentRequest { Input = Quote(source) },
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Stderr);
    }

    [Fact(Explicit = true)]
    public async Task Attachment_DumpAllWritesIntoTheWorkingDirectory()
    {
        var workDir = Directory.CreateTempSubdirectory("jf-dumpall-").FullName;

        try
        {
            var result = await _runner.RunAsync(
                new AttachmentRequest
                {
                    Input = Quote(_media.VideoWithAttachment),
                    WorkingDirectory = workDir
                },
                CancellationToken.None);

            Assert.True(result.Succeeded, result.Stderr);
            Assert.True(File.Exists(Path.Combine(workDir, _media.AttachmentName)));
        }
        finally
        {
            Directory.Delete(workDir, true);
        }
    }

    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe_ProducesParseableJson(bool includeChapters)
    {
        string json = string.Empty;

        var result = await _runner.RunAsync(
            new ProbeRequest
            {
                Input = Quote(_media.Mp4),
                IncludeChapters = includeChapters,
                Stdout = async (stdout, ct) =>
                {
                    using var reader = new StreamReader(stdout);
                    json = await reader.ReadToEndAsync(ct);
                }
            },
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Stderr);

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("streams", out var streams));
        Assert.Contains(streams.EnumerateArray(), s => s.GetProperty("codec_type").GetString() == "video");
        Assert.Equal(includeChapters, document.RootElement.TryGetProperty("chapters", out _));
    }

    [Fact(Explicit = true)]
    public async Task Probe_RejectsNothingItIsGiven()
    {
        // ffprobe accepts neither -nostdin nor -y, and swallows the following argument if given
        // one, so a regression here corrupts the whole command rather than failing cleanly.
        var result = await _runner.RunAsync(
            new ProbeRequest { Input = Quote(_media.VideoWithAttachment), Threads = 2 },
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Stderr);
        Assert.DoesNotContain("Option not found", result.Stderr, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task Cancellation_KillsTheProcess()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _runner.RunAsync(
            new ProbeRequest { Input = Quote(_media.Mp4) },
            cts.Token);

        Assert.False(result.Succeeded);
        Assert.Equal(FFStopReason.Cancelled, result.StopReason);
    }

    [Fact(Explicit = true)]
    public async Task Loudness_MeasurementSurvivesWithoutTheCallerAskingForIt()
    {
        // Guards the log-level floor against a real ebur128, with a request that names no sink at
        // all — as AudioNormalizationTask does. Measured against jellyfin-ffmpeg 7.1.4, the summary
        // is emitted at info and verbose and at nothing below, so without StderrIsPayload raising the
        // derived level this run still exits 0 with the caller's regex matching nothing.
        //
        // It does not cover the sink substitution that flag also performs: ebur128 prints its summary
        // last, so a trailing window would hold it regardless.
        var result = await _runner.RunAsync(
            new LoudnessRequest { Path = _media.Mp4 },
            CancellationToken.None);

        Assert.True(result.Succeeded, result.Stderr);
        Assert.Contains("Summary:", result.Stderr, StringComparison.Ordinal);
        Assert.Contains("I:", result.Stderr, StringComparison.Ordinal);
    }

    [Fact(Explicit = true)]
    public async Task Termination_KillsAWriteThenCloseProcessThatOutlivesItsDeadline()
    {
        // ProbeRuntimeKeys uses FFStdinMode.WriteThenClose: stdin carries its query and is then shut,
        // so there is no way to ask it to quit and termination must go straight to the kill.
        //
        // -re paces the null source in real time, so the run outlives the deadline instead of
        // finishing on its own the way the real startup interrogation does.
        var session = await _runner.StartAsync(
            new RuntimeKeyProbeRequest
            {
                Arguments = "-re -f lavfi -i nullsrc=s=1x1:d=600 -f null -",
                Timeout = TimeSpan.FromSeconds(2)
            },
            CancellationToken.None);

        var processId = session.ProcessId;

        try
        {
            // Bounded, and deliberately not `await using`: both a bare await and disposing the session
            // wait on Completion, which does not finish while the process is alive. A TimeoutException
            // here is the failure this test exists to catch, and the bound is what makes it a failure
            // rather than a hung run.
            var result = await session.Completion.WaitAsync(
                TimeSpan.FromSeconds(30),
                TestContext.Current.CancellationToken);

            Assert.Equal(FFStopReason.TimedOut, result.StopReason);
            Assert.True(HasReallyExited(processId), "ffmpeg survived termination");
        }
        finally
        {
            // Reap anything the runner left behind. A process that survives termination is also gone
            // from _running, so neither the runner's own shutdown nor this class's Dispose collects
            // it, and a failure here would otherwise leak a real ffmpeg into the rest of the run.
            KillIfAlive(processId);
        }
    }

    private static void KillIfAlive(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (ArgumentException)
        {
            // Already gone, which is the expected case.
        }
    }

    /// <summary>
    /// Whether the process is gone, tolerating the id having been recycled into something else.
    /// </summary>
    private static bool HasReallyExited(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);

            return process.HasExited;
        }
        catch (ArgumentException)
        {
            // No such process, which is the answer we want.
            return true;
        }
    }

    private Task<FFResult> RunDumpAsync(string source, int streamIndex, string target)
        => _runner.RunAsync(
            new AttachmentRequest
            {
                Input = Quote(source),
                Targets = [new AttachmentTarget(streamIndex, target)]
            },
            CancellationToken.None);

    private static string Quote(string path) => "file:\"" + path + "\"";
}
