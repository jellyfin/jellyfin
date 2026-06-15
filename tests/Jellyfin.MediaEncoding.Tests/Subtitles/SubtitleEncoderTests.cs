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
        public void GetSubtitleStreamMapIndex_InternalStream_UsesContainerIndex()
        {
            // The probe normalizer drops some container streams (codec-less subtitles, data,
            // attachments) from MediaStreams, so a stream's position in the list can be lower than
            // its real container index. The streams below have indices 0,1,2,6 (3-5 were dropped),
            // so the target text subtitle is at list position 3 but container index 6. Using the
            // position would point -map at the wrong stream (e.g. an interleaved PGS stream) and
            // fail extraction (#15880); the container index must be used instead.
            var target = new MediaStream { Index = 6, Type = MediaStreamType.Subtitle, Codec = "subrip" };
            var mediaSource = new MediaSourceInfo
            {
                MediaStreams = new[]
                {
                    new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "hevc" },
                    new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "eac3" },
                    new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "subrip" },
                    target
                }
            };

            Assert.Equal(6, SubtitleEncoder.GetSubtitleStreamMapIndex(mediaSource, target));
        }

        [Fact]
        public void GetSubtitleStreamMapIndex_ExternalStream_UsesIndexWithinItsOwnFile()
        {
            // An external subtitle is extracted from its own single-stream file, so the index within
            // that file (0 here) is needed rather than the aggregated container index.
            var target = new MediaStream { Index = 7, Type = MediaStreamType.Subtitle, Codec = "subrip", Path = "/media/movie.en.srt", IsExternal = true };
            var mediaSource = new MediaSourceInfo
            {
                MediaStreams = new[]
                {
                    new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "hevc" },
                    new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "eac3" },
                    target
                }
            };

            Assert.Equal(0, SubtitleEncoder.GetSubtitleStreamMapIndex(mediaSource, target));
        }
    }
}
