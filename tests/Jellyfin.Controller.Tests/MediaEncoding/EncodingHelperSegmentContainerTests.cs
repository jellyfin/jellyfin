using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding;

/// <summary>
/// Regression tests for the HLS segment container fallback used when a client does not
/// explicitly request a <c>segmentContainer</c>.
///
/// Bug: the server unconditionally defaulted to mpegts (.ts) segments when no container was
/// requested, even though mpegts cannot carry audio codecs such as TrueHD, DTS, FLAC, ALAC or
/// Opus. This forced the server to transcode those codecs down to AAC instead of stream-copying
/// them, even for clients that had declared HLS/fMP4 support for the source codec.
/// </summary>
public class EncodingHelperSegmentContainerTests
{
    [Theory]
    [InlineData("aac")]
    [InlineData("ac3")]
    [InlineData("eac3")]
    [InlineData("mp3")]
    [InlineData(null)]
    public void GetSegmentFileExtension_NoContainerRequested_TsCompatibleAudio_DefaultsToTs(string? audioCodec)
    {
        Assert.Equal(".ts", EncodingHelper.GetSegmentFileExtension(null, audioCodec));
        Assert.Equal(".ts", EncodingHelper.GetSegmentFileExtension(string.Empty, audioCodec));
    }

    [Theory]
    [InlineData("truehd")]
    [InlineData("dts")]
    [InlineData("flac")]
    [InlineData("alac")]
    [InlineData("opus")]
    [InlineData("TrueHD")]
    public void GetSegmentFileExtension_NoContainerRequested_TsIncompatibleAudio_FallsBackToMp4(string audioCodec)
    {
        Assert.Equal(".mp4", EncodingHelper.GetSegmentFileExtension(null, audioCodec));
        Assert.Equal(".mp4", EncodingHelper.GetSegmentFileExtension(string.Empty, audioCodec));
    }

    [Theory]
    [InlineData("ts", "truehd")]
    [InlineData("mp4", "aac")]
    [InlineData("mkv", "dts")]
    public void GetSegmentFileExtension_ExplicitContainerRequested_IsAlwaysHonored(string requestedContainer, string audioCodec)
    {
        Assert.Equal("." + requestedContainer, EncodingHelper.GetSegmentFileExtension(requestedContainer, audioCodec));
    }

    [Fact]
    public void ActualOutputAudioCodec_StreamCopiedTrueHd_ResolvesToMp4Segments()
    {
        // Mirrors what DynamicHlsController/HlsHelpers see for a real request: the client
        // asked to copy the audio (no server-side transcode needed) and the source audio is
        // TrueHD, so the actual codec ending up on the wire is TrueHD, not the OutputAudioCodec
        // placeholder value of "copy".
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            AudioStream = new MediaStream
            {
                Type = MediaStreamType.Audio,
                Codec = "truehd"
            },
            OutputAudioCodec = "copy",
            BaseRequest = new VideoRequestDto()
        };

        Assert.Equal("truehd", state.ActualOutputAudioCodec);
        Assert.Equal(".mp4", EncodingHelper.GetSegmentFileExtension(null, state.ActualOutputAudioCodec));
    }

    [Fact]
    public void ActualOutputAudioCodec_StreamCopiedAac_ResolvesToTsSegments()
    {
        var state = new EncodingJobInfo(TranscodingJobType.Hls)
        {
            AudioStream = new MediaStream
            {
                Type = MediaStreamType.Audio,
                Codec = "aac"
            },
            OutputAudioCodec = "copy",
            BaseRequest = new VideoRequestDto()
        };

        Assert.Equal("aac", state.ActualOutputAudioCodec);
        Assert.Equal(".ts", EncodingHelper.GetSegmentFileExtension(null, state.ActualOutputAudioCodec));
    }
}
