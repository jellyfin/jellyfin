using System;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class ClipFfmpegArgsBuilderTests
{
    private const string InputPath = "/media/movie.mkv";
    private const string OutputPath = "/transcode/clip-abc.mp4";

    // ── Seek / duration ──────────────────────────────────────────────

    [Fact]
    public void Build_ContainsInputLevelSeek()
    {
        var args = Build();
        Assert.Contains("-ss 10.000000", args, StringComparison.Ordinal);
        Assert.Contains($"-i file:\"{InputPath}\"", args, StringComparison.Ordinal);
        Assert.Contains("-t 30.000000", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_SeekIsBeforeInput()
    {
        var args = Build();
        var ssIdx = args.IndexOf("-ss ", StringComparison.Ordinal);
        var iIdx = args.IndexOf("-i ", StringComparison.Ordinal);
        Assert.True(ssIdx < iIdx, "-ss must appear before -i for input-level seek");
    }

    // ── Stream mapping ───────────────────────────────────────────────

    [Fact]
    public void Build_MapsVideoAndAudio()
    {
        var args = Build(audioStreamIndex: 0);
        Assert.Contains("-map 0:v:0", args, StringComparison.Ordinal);
        Assert.Contains("-map 0:a:0", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_MapsCorrectAudioStreamIndex()
    {
        var args = Build(audioStreamIndex: 2);
        Assert.Contains("-map 0:a:2", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_StripMetadataAndChapters()
    {
        var args = Build();
        Assert.Contains("-map_metadata -1", args, StringComparison.Ordinal);
        Assert.Contains("-map_chapters -1", args, StringComparison.Ordinal);
    }

    // ── Encoder selection ────────────────────────────────────────────

    [Theory]
    [InlineData("libx264", "-preset superfast")]
    [InlineData("libx265", "-preset superfast")]
    [InlineData("libsvtav1", "-preset 8")]
    public void Build_AppliesEncoderSpecificPreset(string encoder, string expectedPresetFragment)
    {
        var args = Build(videoEncoder: encoder);
        Assert.Contains(expectedPresetFragment, args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_AppliesVideoEncoder()
    {
        var args = Build(videoEncoder: "libx265");
        Assert.Contains("-c:v libx265", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_AppliesAudioEncoder()
    {
        var args = Build(audioEncoder: "libopus");
        Assert.Contains("-c:a libopus", args, StringComparison.Ordinal);
    }

    // ── Bitrate ──────────────────────────────────────────────────────

    [Fact]
    public void Build_WithVideoBitRate_SetsTargetAndMaxrate()
    {
        var args = Build(videoEncoder: "libx264", videoBitRate: 4_000_000);
        Assert.Contains("-b:v 4000000", args, StringComparison.Ordinal);
        Assert.Contains("-maxrate 4000000", args, StringComparison.Ordinal);
        Assert.Contains("-bufsize 8000000", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithAv1VideoEncoder_SetsSimpleBitrate()
    {
        var args = Build(videoEncoder: "libsvtav1", videoBitRate: 3_000_000);
        Assert.Contains("-b:v 3000000", args, StringComparison.Ordinal);
        Assert.DoesNotContain("-maxrate", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithoutVideoBitRate_OmitsBitrateFlag()
    {
        var args = Build(videoBitRate: null);
        Assert.DoesNotContain("-b:v", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithAudioBitRate_SetsAudioBitrate()
    {
        var args = Build(audioBitRate: 320_000);
        Assert.Contains("-b:a 320000", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithoutAudioBitRate_OmitsAudioBitrateFlag()
    {
        var args = Build(audioBitRate: null);
        Assert.DoesNotContain("-b:a", args, StringComparison.Ordinal);
    }

    // ── Video filter ─────────────────────────────────────────────────

    [Fact]
    public void Build_ContainsEvenDimensionScaleFilter()
    {
        var args = Build();
        Assert.Contains("scale=trunc(iw/2)*2:trunc(ih/2)*2", args, StringComparison.Ordinal);
    }

    // ── Container-specific flags ─────────────────────────────────────

    [Fact]
    public void Build_Mp4Container_AddsFaststart()
    {
        var args = Build(container: "mp4");
        Assert.Contains("-movflags +faststart", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WebmContainer_NoFaststart()
    {
        var args = Build(container: "webm", videoEncoder: "libvpx-vp9");
        Assert.DoesNotContain("-movflags", args, StringComparison.Ordinal);
    }

    // ── Output path ──────────────────────────────────────────────────

    [Fact]
    public void Build_EndsWithOutputFilePath()
    {
        var args = Build();
        Assert.EndsWith($"file:\"{OutputPath}\"", args, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ContainsOverwriteFlag()
    {
        var args = Build();
        Assert.Contains("-y ", args, StringComparison.Ordinal);
    }

    // ── Helper ───────────────────────────────────────────────────────

    private static string Build(
        string inputPath = InputPath,
        string outputPath = OutputPath,
        double startSeconds = 10.0,
        double durationSeconds = 30.0,
        string videoEncoder = "libx264",
        string audioEncoder = "aac",
        int? videoBitRate = 2_000_000,
        int? audioBitRate = 192_000,
        string container = "mp4",
        int audioStreamIndex = 0)
        => ClipFfmpegArgsBuilder.Build(new ClipFfmpegOptions
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            StartSeconds = startSeconds,
            DurationSeconds = durationSeconds,
            VideoEncoder = videoEncoder,
            AudioEncoder = audioEncoder,
            VideoBitRate = videoBitRate,
            AudioBitRate = audioBitRate,
            Container = container,
            AudioStreamIndex = audioStreamIndex
        });
}
