using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jellyfin.Controller.Tests.Streaming
{
    public class HlsCommandBuilderTests
    {
        private const string MoviePath = "/media/movie.mkv";

        [Fact]
        public void GetCommandLineArguments_VodTs_BuildsAnHlsTranscode()
        {
            var args = Build("ts", isEventPlaylist: false);

            Assert.StartsWith("-i file:\"/media/movie.mkv\" -map_metadata -1 -map_chapters -1 -threads 0", args, StringComparison.Ordinal);
            Assert.Contains("-codec:v:0 libx264", args, StringComparison.Ordinal);
            Assert.Contains("-codec:a:0 aac", args, StringComparison.Ordinal);
            Assert.Contains("-f hls -max_delay 5000000 -hls_time 6 -hls_segment_type mpegts -start_number 0", args, StringComparison.Ordinal);
            Assert.Contains("-hls_segment_filename \"/transcodes/abc%d.ts\" -hls_playlist_type vod -hls_list_size 0", args, StringComparison.Ordinal);
            Assert.EndsWith("-y \"/transcodes/abc.m3u8\"", args, StringComparison.Ordinal);
            Assert.DoesNotContain("-hls_base_url", args, StringComparison.Ordinal);
        }

        [Fact]
        public void GetCommandLineArguments_EventPlaylist_AddsTheBaseUrl()
        {
            var args = Build("ts", isEventPlaylist: true);

            Assert.Contains("-hls_base_url \"hls/abc/\"", args, StringComparison.Ordinal);
            Assert.Contains("-hls_playlist_type event", args, StringComparison.Ordinal);
            Assert.Contains("-flags -global_header", args, StringComparison.Ordinal);
        }

        [Fact]
        public void GetCommandLineArguments_Fmp4_UsesTheFmp4Muxer()
        {
            var args = Build("mp4", isEventPlaylist: false);

            Assert.Contains("-hls_segment_type fmp4 -hls_fmp4_init_filename", args, StringComparison.Ordinal);
            Assert.Contains("-hls_segment_options movflags=+frag_discont+skip_sidx", args, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(8, 1, "smpte2084", false, "hvc1")]
        [InlineData(8, 1, "smpte2084", true, "hvc1")]
        [InlineData(8, 2, "bt709", false, "hvc1")]
        [InlineData(8, 4, "arib-std-b67", false, "hvc1")]
        [InlineData(8, 6, "smpte2084", false, "hvc1")]
        [InlineData(5, 0, "smpte2084", false, "dvh1")]
        [InlineData(7, 6, "smpte2084", false, "dvh1")]
        [InlineData(null, null, "smpte2084", false, "dvh1")]
        public void GetDolbyVisionHevcCodecTag_SelectsTagForProfile(int? profile, int? compatibilityId, string transfer, bool hdr10Plus, string expected)
        {
            var stream = new MediaStream
            {
                Codec = "hevc",
                Type = MediaStreamType.Video,
                RpuPresentFlag = 1,
                BlPresentFlag = 1,
                ColorSpace = "bt2020nc",
                ColorPrimaries = "bt2020",
                ColorTransfer = transfer,
                DvProfile = profile,
                DvBlSignalCompatibilityId = compatibilityId,
                Hdr10PlusPresentFlag = hdr10Plus
            };

            Assert.Equal(expected, HlsCommandBuilder.GetDolbyVisionHevcCodecTag(stream));
        }

        private static string Build(string segmentContainer, bool isEventPlaylist)
        {
            var mediaEncoder = new Mock<IMediaEncoder>();
            mediaEncoder.SetupGet(e => e.EncoderVersion).Returns(new Version(7, 1));
            mediaEncoder.Setup(e => e.GetInputPathArgument(It.IsAny<EncodingJobInfo>())).Returns("file:\"" + MoviePath + "\"");
            var encodingHelper = new EncodingHelper(
                Mock.Of<IApplicationPaths>(),
                mediaEncoder.Object,
                Mock.Of<ISubtitleEncoder>(),
                new Mock<IConfiguration>().Object,
                Mock.Of<IConfigurationManager>(),
                Mock.Of<IPathManager>());
            var builder = new HlsCommandBuilder(encodingHelper, mediaEncoder.Object, NullLogger<HlsCommandBuilder>.Instance);

            var video = new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 1920, Height = 1080, PixelFormat = "yuv420p", BitDepth = 8 };
            var audio = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac", Channels = 2 };
            using var state = new StreamState(Mock.Of<IMediaSourceManager>(), TranscodingJobType.Hls, Mock.Of<ITranscodeManager>())
            {
                Request = new VideoRequestDto { SegmentContainer = segmentContainer, SegmentLength = 6, VideoCodec = "h264", AudioCodec = "aac", MaxWidth = 1280 },
                MediaSource = new MediaSourceInfo
                {
                    Container = "mkv",
                    Path = MoviePath,
                    Protocol = MediaProtocol.File,
                    MediaStreams = new List<MediaStream> { video, audio }
                },
                MediaPath = MoviePath,
                VideoStream = video,
                AudioStream = audio,
                IsInputVideo = true,
                OutputVideoCodec = "h264",
                OutputVideoBitrate = 4_000_000,
                OutputAudioCodec = "aac",
                OutputAudioBitrate = 192_000,
                OutputAudioChannels = 2
            };

            return builder.GetCommandLineArguments("/transcodes/abc.m3u8", state, new EncodingOptions(), isEventPlaylist, 0);
        }
    }
}
