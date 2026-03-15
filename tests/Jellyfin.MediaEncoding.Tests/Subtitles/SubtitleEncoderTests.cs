using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
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
        public async Task GetSubtitleFileCharacterSet_HttpMediaSourceWithLocalSubtitle_DoesNotUseHttpClient()
        {
            var subtitlePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".srt");
            await File.WriteAllTextAsync(subtitlePath, "1\n00:00:00,000 --> 00:00:01,000\ntest\n");

            try
            {
                var mediaSourceManagerMock = new Mock<IMediaSourceManager>();
                mediaSourceManagerMock
                    .Setup(x => x.GetPathProtocol(subtitlePath))
                    .Returns(MediaProtocol.File);

                var httpClientFactoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);

                var subtitleEncoder = new SubtitleEncoder(
                    Mock.Of<ILogger<SubtitleEncoder>>(),
                    Mock.Of<IFileSystem>(),
                    Mock.Of<IMediaEncoder>(),
                    httpClientFactoryMock.Object,
                    mediaSourceManagerMock.Object,
                    Mock.Of<ISubtitleParser>(),
                    Mock.Of<IPathManager>(),
                    Mock.Of<IServerConfigurationManager>());

                await subtitleEncoder.GetSubtitleFileCharacterSet(
                    new MediaStream
                    {
                        Path = subtitlePath,
                        IsExternal = true,
                        Codec = "srt"
                    },
                    "eng",
                    new MediaSourceInfo
                    {
                        Protocol = MediaProtocol.Http
                    },
                    CancellationToken.None);

                mediaSourceManagerMock.Verify(x => x.GetPathProtocol(subtitlePath), Times.Once);
                httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
            }
            finally
            {
                File.Delete(subtitlePath);
            }
        }
    }
}
