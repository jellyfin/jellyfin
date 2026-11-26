using System.Text;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class DynamicHlsHelperTests
    {
        [Theory]
        [MemberData(nameof(GetVttTimestampMapMpegts_Success_TestData))]
        public static void GetVttTimestampMapMpegts_Success(string? segmentContainer, long expected)
        {
            Assert.Equal(expected, DynamicHlsHelper.GetVttTimestampMapMpegts(segmentContainer));
        }

        public static TheoryData<string?, long> GetVttTimestampMapMpegts_Success_TestData()
        {
            var data = new TheoryData<string?, long>();
            data.Add(null, 900000);
            data.Add(string.Empty, 900000);
            data.Add("ts", 900000);
            data.Add("mp4", 0);
            data.Add("MP4", 0);
            return data;
        }

        [Fact]
        public static void AppendAudioRendition_SeparateAudio_AppendsOneGroupPerVariant()
        {
            var state = CreateState("mp4", true);
            var builder = new StringBuilder();

            var group = DynamicHlsHelper.AppendAudioRendition(builder, state, "main.m3u8?VideoCodec=h264&SeparateAudioRendition=true");
            var sameGroup = DynamicHlsHelper.AppendAudioRendition(builder, state, "main.m3u8?VideoCodec=h264&SeparateAudioRendition=true");
            var otherGroup = DynamicHlsHelper.AppendAudioRendition(builder, state, "main.m3u8?VideoCodec=hevc&SeparateAudioRendition=true");

            Assert.NotNull(group);
            Assert.Equal(group, sameGroup);
            Assert.NotEqual(group, otherGroup);
            var lines = builder.ToString().TrimEnd().Split('\n');
            Assert.Equal(2, lines.Length);
            Assert.Equal(
                "#EXT-X-MEDIA:TYPE=AUDIO,GROUP-ID=\"" + group + "\",NAME=\"" + state.AudioStream.DisplayTitle + "\",DEFAULT=YES,AUTOSELECT=YES,LANGUAGE=\"eng\",CHANNELS=\"2\",URI=\"audio.m3u8?VideoCodec=h264&SeparateAudioRendition=true\"",
                lines[0].TrimEnd('\r'));
        }

        [Theory]
        [InlineData("ts", true)]
        [InlineData("mp4", false)]
        public static void AppendAudioRendition_NotEnabled_AppendsNothing(string segmentContainer, bool separateAudioRendition)
        {
            var state = CreateState(segmentContainer, separateAudioRendition);
            var builder = new StringBuilder();

            Assert.Null(DynamicHlsHelper.AppendAudioRendition(builder, state, "main.m3u8?SeparateAudioRendition=true"));
            Assert.Equal(0, builder.Length);
        }

        private static StreamState CreateState(string segmentContainer, bool separateAudioRendition)
            => new StreamState(Mock.Of<IMediaSourceManager>(), TranscodingJobType.Hls, Mock.Of<ITranscodeManager>())
            {
                Request = new VideoRequestDto
                {
                    SegmentContainer = segmentContainer,
                    SeparateAudioRendition = separateAudioRendition
                },
                RunTimeTicks = 600_000_000,
                VideoStream = new MediaStream { Type = MediaStreamType.Video, Codec = "h264" },
                AudioStream = new MediaStream { Type = MediaStreamType.Audio, Codec = "aac", Language = "eng", Channels = 2 },
                OutputAudioChannels = 2,
                SubtitleDeliveryMethod = SubtitleDeliveryMethod.External
            };
    }
}
