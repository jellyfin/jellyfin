using System;
using System.Text;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding.FFProcessing;

public static class FFRequestTests
{
    private static string Args(FFRequest request)
    {
        var builder = new StringBuilder();
        request.BuildArguments(builder);
        return builder.ToString();
    }

    [Theory]
    [InlineData(FFAction.Probe)]
    [InlineData(FFAction.ScanKeyframes)]
    [InlineData(FFAction.Capabilities)]
    [InlineData(FFAction.ProbeRuntimeKeys)]
    [InlineData(FFAction.MeasureLoudness)]
    [InlineData(FFAction.ExtractAttachment)]
    [InlineData(FFAction.ExtractSubtitle)]
    [InlineData(FFAction.ExtractImage)]
    [InlineData(FFAction.GenerateTrickplay)]
    [InlineData(FFAction.Record)]
    public static void Policy_EveryActionHasOne(FFAction action)
    {
        var policy = FFActionPolicy.For(action);

        Assert.NotEqual(TimeSpan.Zero, policy.Timeout);
        Assert.NotEqual(TimeSpan.Zero, policy.IdleTimeout);
        Assert.NotEqual(TimeSpan.Zero, policy.Timeout);
    }

    [Fact]
    public static void Policy_OnlyProbesAreProbeOnly()
    {
        Assert.True(FFActionPolicy.For(FFAction.Probe).ProbeOnly);
        Assert.True(FFActionPolicy.For(FFAction.ScanKeyframes).ProbeOnly);
        Assert.False(FFActionPolicy.For(FFAction.ExtractImage).ProbeOnly);
    }

    [Theory]
    [InlineData(FFAction.Probe)]
    [InlineData(FFAction.ScanKeyframes)]
    [InlineData(FFAction.Capabilities)]
    [InlineData(FFAction.ProbeRuntimeKeys)]
    [InlineData(FFAction.MeasureLoudness)]
    [InlineData(FFAction.ExtractAttachment)]
    [InlineData(FFAction.ExtractSubtitle)]
    [InlineData(FFAction.ExtractImage)]
    [InlineData(FFAction.GenerateTrickplay)]
    [InlineData(FFAction.Record)]
    public static void Policy_ProbeOnlyImpliesFireAndForget(FFAction action)
    {
        // ffprobe has no runtime-key interface, so a steerable probe is not a thing.
        var policy = FFActionPolicy.For(action);

        if (policy.ProbeOnly)
        {
            Assert.Equal(FFStdinMode.FireAndForget, policy.Stdin);
        }
    }

    [Fact]
    public static void Request_ProbeOnlyOverrideCannotMakeItSteerable()
    {
        // ProbeOnly is the one policy field a request may override, so cover that path too.
        var request = new CapabilitiesRequest { Arguments = "-hwaccels", ProbeOnly = true };
        var policy = request.ResolvePolicy();

        Assert.True(policy.ProbeOnly);
        Assert.Equal(FFStdinMode.FireAndForget, policy.Stdin);
    }

    [Fact]
    public static void Policy_OnlyRecordIsSteerableByDefault()
    {
        Assert.Equal(FFStdinMode.ControlChannel, FFActionPolicy.For(FFAction.Record).Stdin);
        Assert.Equal(FFStdinMode.FireAndForget, FFActionPolicy.For(FFAction.GenerateTrickplay).Stdin);
        Assert.Equal(FFStdinMode.FireAndForget, FFActionPolicy.For(FFAction.Probe).Stdin);
    }

    [Theory]
    [InlineData(FFAction.MeasureLoudness)]
    [InlineData(FFAction.Capabilities)]
    [InlineData(FFAction.ProbeRuntimeKeys)]
    public static void Policy_ActionsThatParseTheirOwnStderrSaySo(FFAction action)
    {
        // These read stderr as the answer. Without the flag the runner derives -loglevel from the
        // server level, and at the default that is quiet enough to suppress the answer entirely —
        // silently, since ffmpeg still exits 0.
        Assert.True(FFActionPolicy.For(action).StderrIsPayload);
    }

    [Theory]
    [InlineData(FFAction.Probe)]
    [InlineData(FFAction.ScanKeyframes)]
    [InlineData(FFAction.ExtractAttachment)]
    [InlineData(FFAction.ExtractSubtitle)]
    [InlineData(FFAction.ExtractImage)]
    [InlineData(FFAction.GenerateTrickplay)]
    [InlineData(FFAction.Record)]
    public static void Policy_ActionsWhoseStderrIsOnlyDiagnosticSaySo(FFAction action)
    {
        Assert.False(FFActionPolicy.For(action).StderrIsPayload);
    }

    [Fact]
    public static void DefaultResult_DoesNotClaimSuccess()
    {
        // FFResult is a record struct, so FFStopReason.Unknown occupies zero specifically so that state reports failure.
        Assert.False(default(FFResult).Succeeded);
    }

    [Fact]
    public static void Request_TimeoutAndPriorityFallBackToTheAction()
    {
        var request = new ProbeRequest { Input = "file:\"a.mkv\"" };
        var policy = request.ResolvePolicy();

        Assert.Equal(FFActionPolicy.For(FFAction.Probe).Timeout, policy.Timeout);
        Assert.Equal(FFActionPolicy.For(FFAction.Probe).Priority, policy.Priority);
    }

    [Fact]
    public static void Request_ExplicitTimeoutWins()
    {
        var request = new ProbeRequest { Input = "file:\"a.mkv\"", Timeout = TimeSpan.FromSeconds(7) };
        var policy = request.ResolvePolicy();

        Assert.Equal(TimeSpan.FromSeconds(7), policy.Timeout);
    }

    [Fact]
    public static void RuntimeKeyProbe_IsSteerableAndPlainCapabilitiesIsNot()
    {
        // The runtime-key test cannot run with -nostdin, because that disables what it is testing.
        var probe = new RuntimeKeyProbeRequest { Arguments = "-f lavfi -i nullsrc" };
        var capabilities = new CapabilitiesRequest { Arguments = "-hwaccels" };

        Assert.Equal(FFStdinMode.ControlChannel, probe.ResolvePolicy().Stdin);
        Assert.Equal(FFStdinMode.FireAndForget, capabilities.ResolvePolicy().Stdin);
    }

    [Fact]
    public static void Capabilities_CanTargetTheProber()
    {
        var request = new CapabilitiesRequest { Arguments = "-loglevel quiet", ProbeOnly = true };
        var policy = request.ResolvePolicy();

        Assert.True(policy.ProbeOnly);
    }

    [Theory]
    [InlineData(FFDelivery.Progressive, StreamMode.Transcode)]
    [InlineData(FFDelivery.Progressive, StreamMode.Remux)]
    [InlineData(FFDelivery.Hls, StreamMode.DirectStream)]
    [InlineData(FFDelivery.Hls, StreamMode.Remux)]
    public static void Stream_AlwaysOverwrites(FFDelivery delivery, StreamMode mode)
    {
        // The runner is the only source of the overwrite flag, and delivery routinely writes over
        // output an abandoned session left behind. If -n were ever emitted, FFmpeg would refuse the
        // write and still exit 0 — playback silently dead with nothing in the log. StreamRequest
        // pins Overwrite on whatever the policy table says; this holds it there.
        var request = new StreamRequest
        {
            Delivery = delivery,
            Mode = mode,
            Arguments = "-i \"in.mkv\" -c:v copy -c:a copy \"out.mkv\""
        };

        Assert.True(request.ResolvePolicy().Overwrite);
    }

    [Fact]
    public static void Probe_BuildsExpectedArguments()
    {
        var request = new ProbeRequest
        {
            Input = "file:\"a.mkv\"",
            SourceTuning = "-analyzeduration 5000000 -probesize 1000000",
            IncludeChapters = true,
            Threads = 4
        };

        Assert.Equal(
            "-analyzeduration 5000000 -probesize 1000000 -i file:\"a.mkv\" -threads 4 "
            + "-print_format json -show_streams -show_chapters -show_format",
            Args(request));
    }

    [Fact]
    public static void Probe_ZeroThreadsOmitsTheFlag()
    {
        var request = new ProbeRequest { Input = "file:\"a.mkv\"" };
        var args = Args(request);

        Assert.DoesNotContain("-threads", args, StringComparison.Ordinal);
    }

    [Fact]
    public static void Attachment_NoTargetsDumpsAllByEmbeddedName()
    {
        var request = new AttachmentRequest { Input = "file:\"a.mkv\"" };
        var args = Args(request);

        Assert.Equal(
            "-dump_attachment:t \"\" -i file:\"a.mkv\" -map 0:t? -t 0 -f null null",
            args);
    }

    [Fact]
    public static void Attachment_TargetsAreDumpedByIndex()
    {
        var request = new AttachmentRequest
        {
            Input = "file:\"a.mkv\"",
            Targets = [new AttachmentTarget(3, "/f/one.ttf"), new AttachmentTarget(4, "/f/two.ttf")]
        };
        var args = Args(request);

        Assert.Equal(
            "-dump_attachment:3 \"/f/one.ttf\" -dump_attachment:4 \"/f/two.ttf\" "
            + "-i file:\"a.mkv\" -map 0:t? -t 0 -f null null",
            args);
    }

    [Fact]
    public static void Attachment_ShapeIsIdenticalWhateverTheSourceContains()
    {
        // -map 0:t? means subtitle-only containers and attachment-less sources take the same
        // command shape as an A/V file, and all three exit 0.
        var request = new AttachmentRequest
        {
            Input = "file:\"subs.mks\"",
            Targets = [new AttachmentTarget(2, "/f/x.ttf")]
        };
        var args = Args(request);

        Assert.EndsWith("-i file:\"subs.mks\" -map 0:t? -t 0 -f null null", args, StringComparison.Ordinal);
    }

    [Fact]
    public static void Attachment_ConcatPlaylistGetsDemuxerFlags()
    {
        var request = new AttachmentRequest { Input = "file:\"a.concat\"", IsConcatPlaylist = true };
        var args = Args(request);

        Assert.Contains("-f concat -safe 0 -i", args, StringComparison.Ordinal);
    }

    [Fact]
    public static void Watchdog_OnlyRunsWhenARealProgressSignalExists()
    {
        // CPU time is not liveness: work blocked on a slow network read accrues none.
        Assert.False(new ProbeRequest { Input = "x" }.HasProgressSignal);
        Assert.True(new ProbeRequest { Input = "x", ProgressProbe = () => 1 }.HasProgressSignal);
    }

    [Fact]
    public static void Policy_OnlyTrickplayWatchesForIdleness()
    {
        Assert.NotEqual(FFDefaults.Unbounded, FFActionPolicy.For(FFAction.GenerateTrickplay).IdleTimeout);
        Assert.Equal(FFDefaults.Unbounded, FFActionPolicy.For(FFAction.Probe).IdleTimeout);
        Assert.Equal(FFDefaults.Unbounded, FFActionPolicy.For(FFAction.ScanKeyframes).IdleTimeout);
    }

    [Fact]
    public static void Policy_EveryNonStreamingActionHasAWallClock()
    {
        // These bound the run on their own, so none of them may be unbounded.
        FFAction[] bounded =
        [
            FFAction.Probe,
            FFAction.ScanKeyframes,
            FFAction.Capabilities,
            FFAction.MeasureLoudness,
            FFAction.ExtractAttachment,
            FFAction.ExtractSubtitle,
            FFAction.ExtractImage
        ];

        foreach (var action in bounded)
        {
            var policy = FFActionPolicy.For(action);
            Assert.NotEqual(FFDefaults.Unbounded, policy.Timeout);
        }
    }

    [Fact]
    public static void KeyframeScan_SelectsVideoAndCsv()
    {
        var args = Args(new KeyframeScanRequest { FilePath = "/m/a.mkv" });

        Assert.Contains("-skip_frame nokey", args, StringComparison.Ordinal);
        Assert.Contains("-select_streams v -of csv \"/m/a.mkv\"", args, StringComparison.Ordinal);
    }
}
