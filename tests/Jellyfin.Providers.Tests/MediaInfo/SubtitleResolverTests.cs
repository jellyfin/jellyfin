#pragma warning disable CA1002 // Do not expose generic lists

using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class SubtitleResolverTests
    {
        public static TheoryData<List<MediaStream>, string, int, string[], MediaStream[]> AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles_TestData()
        {
            var data = new TheoryData<List<MediaStream>, string, int, string[], MediaStream[]>();

            var index = 0;
            data.Add(
                new List<MediaStream>(),
                "/video/My.Video.mkv",
                index,
                new[]
                {
                    "/video/My.Video.mp3",
                    "/video/My.Video.png",
                    "/video/My.Video.srt",
                    "/video/My.Video.txt",
                    "/video/My.Video.vtt",
                    "/video/My.Video.ass",
                    "/video/My.Video.sub",
                    "/video/My.Video.ssa",
                    "/video/My.Video.smi",
                    "/video/My.Video.sami",
                    "/video/My.Video.en.srt",
                    "/video/My.Video.default.en.srt",
                    "/video/My.Video.default.forced.en.srt",
                    "/video/My.Video.en.default.forced.srt",
                    "/video/My.Video.With.Additional.Garbage.en.srt",
                    "/video/My.Video With Additional Garbage.srt"
                },
                new[]
                {
                    CreateMediaStream("/video/My.Video.srt", "srt", null, index++),
                    CreateMediaStream("/video/My.Video.vtt", "vtt", null, index++),
                    CreateMediaStream("/video/My.Video.ass", "ass", null, index++),
                    CreateMediaStream("/video/My.Video.sub", "sub", null, index++),
                    CreateMediaStream("/video/My.Video.ssa", "ssa", null, index++),
                    CreateMediaStream("/video/My.Video.smi", "smi", null, index++),
                    CreateMediaStream("/video/My.Video.sami", "sami", null, index++),
                    CreateMediaStream("/video/My.Video.en.srt", "srt", "en", index++),
                    CreateMediaStream("/video/My.Video.default.en.srt", "srt", "en", index++, isDefault: true),
                    CreateMediaStream("/video/My.Video.default.forced.en.srt", "srt", "en", index++, isForced: true, isDefault: true),
                    CreateMediaStream("/video/My.Video.en.default.forced.srt", "srt", "en", index++, isForced: true, isDefault: true),
                    CreateMediaStream("/video/My.Video.With.Additional.Garbage.en.srt", "srt", "en", index),
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles_TestData))]
        public void AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles(List<MediaStream> streams, string videoPath, int startIndex, string[] files, MediaStream[] expectedResult)
        {
            new SubtitleResolver(Mock.Of<ILocalizationManager>()).AddExternalSubtitleStreams(streams, videoPath, startIndex, files);

            Assert.Equal(expectedResult.Length, streams.Count);
            for (var i = 0; i < expectedResult.Length; i++)
            {
                var expected = expectedResult[i];
                var actual = streams[i];

                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Type, actual.Type);
                Assert.Equal(expected.IsExternal, actual.IsExternal);
                Assert.Equal(expected.Path, actual.Path);
                Assert.Equal(expected.IsDefault, actual.IsDefault);
                Assert.Equal(expected.IsForced, actual.IsForced);
                Assert.Equal(expected.Language, actual.Language);
            }
        }

        [Theory]
        [InlineData("/video/My Video.mkv", "/video/My Video.srt", "srt", null, false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.srt", "srt", null, false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.foreign.srt", "srt", null, true, false)]
        [InlineData("/video/My Video.mkv", "/video/My Video.forced.srt", "srt", null, true, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.srt", "srt", null, false, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.forced.default.srt", "srt", null, true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.srt", "srt", "en", false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.en.srt", "srt", "en", false, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.forced.en.srt", "srt", "en", true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.default.forced.srt", "srt", "en", true, true)]
        public void AddExternalSubtitleStreams_GivenSingleFile_ReturnsExpectedSubtitle(string videoPath, string file, string codec, string? language, bool isForced, bool isDefault)
        {
            var streams = new List<MediaStream>();
            var expected = CreateMediaStream(file, codec, language, 0, isForced, isDefault);

            new SubtitleResolver(Mock.Of<ILocalizationManager>()).AddExternalSubtitleStreams(streams, videoPath, 0, new[] { file });

            Assert.Single(streams);

            var actual = streams[0];

            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.IsExternal, actual.IsExternal);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.IsDefault, actual.IsDefault);
            Assert.Equal(expected.IsForced, actual.IsForced);
            Assert.Equal(expected.Language, actual.Language);
        }

        private static MediaStream CreateMediaStream(string path, string codec, string? language, int index, bool isForced = false, bool isDefault = false)
        {
            return new()
            {
                Index = index,
                Codec = codec,
                Type = MediaStreamType.Subtitle,
                IsExternal = true,
                Path = path,
                IsDefault = isDefault,
                IsForced = isForced,
                Language = language
            };
        }
    }
}
