using System;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class StreamingHelpersTests
    {
        [Theory]
        [InlineData("/media/show/episode.mp4", "mov,mp4,m4a,3gp,3g2,mj2", ".mp4")]
        [InlineData("/media/show/episode.MP4", "mov, mp4, m4a, 3gp, 3g2, mj2", ".MP4")]
        [InlineData("/media/show/episode.m4v", "mov,mp4,m4a,3gp,3g2,mj2", ".mov")]
        [InlineData("/media/song.m4a", "mov,mp4,m4a,3gp,3g2,mj2", ".m4a")]
        [InlineData("/media/show/episode", "mov,mp4,m4a,3gp,3g2,mj2", ".mov")]
        [InlineData(null, "mov,mp4,m4a,3gp,3g2,mj2", ".mov")]
        public static void GetOutputFileExtension_ExtensionlessProgressiveRoute_PrefersMatchingMediaSourceFileExtension(string? path, string container, string expected)
        {
            var state = CreateVideoStreamState();
            state.RequestedUrl = $"/Videos/{Guid.Empty}/stream";
            var mediaSource = new MediaSourceInfo
            {
                Path = path,
                Container = container
            };

            Assert.Equal(expected, StreamingHelpers.GetOutputFileExtension(state, mediaSource));
        }

        [Fact]
        public static void GetOutputFileExtension_RequestedExtension_ReturnsRequestedExtension()
        {
            var state = CreateVideoStreamState();
            state.RequestedUrl = "stream.mkv";

            var mediaSource = new MediaSourceInfo
            {
                Path = "/media/show/episode.mp4",
                Container = "mov,mp4,m4a,3gp,3g2,mj2"
            };

            Assert.Equal(".mkv", StreamingHelpers.GetOutputFileExtension(state, mediaSource));
        }

        [Theory]
        [InlineData("h264", ".ts")]
        [InlineData("hevc", ".mp4")]
        [InlineData("vp9", ".webm")]
        [InlineData("AV1", ".mp4")]
        [InlineData("THEORA", ".ogv")]
        [InlineData("VP8", ".webm")]
        [InlineData("VPX", ".webm")]
        [InlineData("WMV", ".asf")]
        [InlineData("unknown", ".mov")]
        [InlineData(null, ".mov")]
        public static void GetOutputFileExtension_RequestedCodec_TakesPrecedence(string? codec, string expected)
        {
            var state = CreateVideoStreamState();
            state.Request.VideoCodec = codec;
            var mediaSource = new MediaSourceInfo
            {
                Path = "/media/show/episode.mov",
                Container = "mov,mp4,m4a,3gp,3g2,mj2"
            };

            Assert.Equal(expected, StreamingHelpers.GetOutputFileExtension(state, mediaSource));
        }

        [Theory]
        [InlineData("AAC", ".aac")]
        [InlineData("MP3", ".mp3")]
        [InlineData("VORBIS", ".ogg")]
        [InlineData("WMA", ".wma")]
        [InlineData("unknown", ".m4a")]
        [InlineData(null, ".m4a")]
        public static void GetOutputFileExtension_AudioCodec_TakesPrecedence(string? codec, string expected)
        {
            var state = CreateVideoStreamState();
            state.Request = new StreamingRequestDto { AudioCodec = codec };
            var mediaSource = new MediaSourceInfo
            {
                Path = "/media/song.m4a",
                Container = "mov,mp4,m4a"
            };

            Assert.Equal(expected, StreamingHelpers.GetOutputFileExtension(state, mediaSource));
        }

        [Fact]
        public static void GetOutputFileExtension_NoExtensionCodecOrContainer_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => StreamingHelpers.GetOutputFileExtension(CreateVideoStreamState(), null));
        }

        private static StreamState CreateVideoStreamState()
        {
            return new StreamState(
                Mock.Of<IMediaSourceManager>(),
                TranscodingJobType.Progressive,
                Mock.Of<ITranscodeManager>())
            {
                RequestedUrl = "stream",
                Request = new VideoRequestDto()
            };
        }
    }
}
