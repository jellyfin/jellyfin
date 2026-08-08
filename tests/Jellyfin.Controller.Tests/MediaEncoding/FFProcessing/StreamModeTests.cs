using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding.FFProcessing;

public static class StreamModeTests
{
    private static StreamState StateFor(bool videoRequested, string outputVideoCodec, string outputAudioCodec)
    {
        var state = new StreamState(
            Mock.Of<IMediaSourceManager>(),
            TranscodingJobType.Progressive,
            Mock.Of<ITranscodeManager>())
        {
            OutputVideoCodec = outputVideoCodec,
            OutputAudioCodec = outputAudioCodec
        };

        // VideoRequest is derived from the request's runtime type.
        state.Request = videoRequested ? new VideoRequestDto() : new StreamingRequestDto();

        return state;
    }

    [Theory]
    // Video requested: the video codec decides, then the audio codec breaks the tie.
    [InlineData(true, "copy", "copy", StreamMode.Remux)]
    [InlineData(true, "copy", "aac", StreamMode.DirectStream)]
    [InlineData(true, "h264", "copy", StreamMode.Transcode)]
    [InlineData(true, "h264", "aac", StreamMode.Transcode)]
    // Audio only: there is no video to copy, so copying the audio is the whole job.
    [InlineData(false, "", "copy", StreamMode.Remux)]
    [InlineData(false, "", "aac", StreamMode.Transcode)]
    public static void StreamMode_MatchesTheDeliveryItDescribes(
        bool videoRequested,
        string outputVideoCodec,
        string outputAudioCodec,
        StreamMode expected)
    {
        Assert.Equal(expected, StateFor(videoRequested, outputVideoCodec, outputAudioCodec).StreamMode);
    }

    [Fact]
    public static void StreamMode_FollowsALateCodecChange()
    {
        // TryStreamCopy runs again once a live stream is opened, and the HLS master-playlist
        // builder rewrites the video codec and restores it. A cached mode would be wrong for both.
        var state = StateFor(true, "copy", "copy");
        Assert.Equal(StreamMode.Remux, state.StreamMode);

        state.OutputVideoCodec = "h264";
        Assert.Equal(StreamMode.Transcode, state.StreamMode);

        state.OutputVideoCodec = "copy";
        Assert.Equal(StreamMode.Remux, state.StreamMode);
    }
}
