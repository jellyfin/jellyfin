using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class SubtitleEncoderTests
    {
        public static TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo> GetReadableFile_Valid_TestData()
        {
            var data = new TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo>();

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.ass",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ass",
                    Protocol = MediaProtocol.File,
                    Format = "ass",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.ssa",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ssa",
                    Protocol = MediaProtocol.File,
                    Format = "ssa",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.srt",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.srt",
                    Protocol = MediaProtocol.File,
                    Format = "srt",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.Http
                },
                new MediaStream()
                {
                    Path = "/media/sub.ass",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ass",
                    Protocol = MediaProtocol.File,
                    Format = "ass",
                    IsExternal = true
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(GetReadableFile_Valid_TestData))]
        public async Task GetReadableFile_Valid_Success(MediaSourceInfo mediaSource, MediaStream subtitleStream, SubtitleEncoder.SubtitleInfo subtitleInfo)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            var subtitleEncoder = fixture.Create<SubtitleEncoder>();
            var result = await subtitleEncoder.GetReadableFile(mediaSource, subtitleStream, CancellationToken.None);
            Assert.Equal(subtitleInfo.Path, result.Path);
            Assert.Equal(subtitleInfo.Protocol, result.Protocol);
            Assert.Equal(subtitleInfo.Format, result.Format);
            Assert.Equal(subtitleInfo.IsExternal, result.IsExternal);
        }

        [Fact]
        public async Task ExtractAllExtractableSubtitles_InfiniteStream_SkipsExtraction()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            var mediaEncoder = fixture.Freeze<Mock<IMediaEncoder>>();
            var subtitleEncoder = fixture.Create<SubtitleEncoder>();

            var mediaSource = new MediaSourceInfo
            {
                IsInfiniteStream = true,
                Path = "https://example.com/live/stream.ts",
                Protocol = MediaProtocol.Http,
                MediaStreams =
                [
                    new MediaStream
                    {
                        Index = 2,
                        Type = MediaStreamType.Subtitle,
                        Codec = "subrip",
                        SupportsExternalStream = true,
                        Path = "https://example.com/live/stream.ts",
                    },
                ],
            };

            await subtitleEncoder.ExtractAllExtractableSubtitles(mediaSource, CancellationToken.None);

            mediaEncoder.Verify(
                m => m.GetInputArgument(It.IsAny<string>(), It.IsAny<MediaSourceInfo>()),
                Times.Never);
        }
    }
}
