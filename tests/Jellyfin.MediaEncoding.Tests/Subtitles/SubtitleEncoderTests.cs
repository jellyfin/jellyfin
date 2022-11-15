using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
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
                new SubtitleEncoder.SubtitleInfo("/media/sub.ass", MediaProtocol.File, "ass", true));

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
                new SubtitleEncoder.SubtitleInfo("/media/sub.ssa", MediaProtocol.File, "ssa", true));

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
                new SubtitleEncoder.SubtitleInfo("/media/sub.srt", MediaProtocol.File, "srt", true));

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
                new SubtitleEncoder.SubtitleInfo("/media/sub.ass", MediaProtocol.File, "ass", true));

            return data;
        }

        [Theory]
        [MemberData(nameof(GetReadableFile_Valid_TestData))]
        internal async Task GetReadableFile_Valid_Success(MediaSourceInfo mediaSource, MediaStream subtitleStream, SubtitleEncoder.SubtitleInfo subtitleInfo)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            var subtitleEncoder = fixture.Create<SubtitleEncoder>();
            var result = await subtitleEncoder.GetReadableFile(mediaSource, subtitleStream, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(subtitleInfo.Path, result.Path);
            Assert.Equal(subtitleInfo.Protocol, result.Protocol);
            Assert.Equal(subtitleInfo.Format, result.Format);
            Assert.Equal(subtitleInfo.IsExternal, result.IsExternal);
        }
    }
}
