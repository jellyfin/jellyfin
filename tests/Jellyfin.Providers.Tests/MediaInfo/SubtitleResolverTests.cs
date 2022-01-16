#pragma warning disable CA1002 // Do not expose generic lists

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class SubtitleResolverTests
    {
        private readonly ILocalizationManager _localizationManager;

        public SubtitleResolverTests()
        {
            var englishCultureDto = new CultureDto()
            {
                Name = "English",
                DisplayName = "English",
                ThreeLetterISOLanguageNames = new[] { "eng" },
                TwoLetterISOLanguageName = "en"
            };
            var frenchCultureDto = new CultureDto()
            {
                Name = "French",
                DisplayName = "French",
                ThreeLetterISOLanguageNames = new[] { "fre", "fra" },
                TwoLetterISOLanguageName = "fr"
            };

            var localizationManager = new Mock<ILocalizationManager>(MockBehavior.Loose);
            localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex(@"en.*", RegexOptions.IgnoreCase)))
                .Returns(englishCultureDto);
            localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex(@"fr.*", RegexOptions.IgnoreCase)))
                .Returns(frenchCultureDto);
            _localizationManager = localizationManager.Object;
        }

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
                    CreateMediaStream("/video/My.Video.srt", "srt", null, null, index++),
                    CreateMediaStream("/video/My.Video.vtt", "vtt", null, null, index++),
                    CreateMediaStream("/video/My.Video.ass", "ass", null, null, index++),
                    CreateMediaStream("/video/My.Video.sub", "sub", null, null, index++),
                    CreateMediaStream("/video/My.Video.ssa", "ssa", null, null, index++),
                    CreateMediaStream("/video/My.Video.smi", "smi", null, null, index++),
                    CreateMediaStream("/video/My.Video.sami", "sami", null, null, index++),
                    CreateMediaStream("/video/My.Video.en.srt", "srt", "eng", null, index++),
                    CreateMediaStream("/video/My.Video.default.en.srt", "srt", "eng", null, index++, isDefault: true),
                    CreateMediaStream("/video/My.Video.default.forced.en.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                    CreateMediaStream("/video/My.Video.en.default.forced.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                    CreateMediaStream("/video/My.Video.With.Additional.Garbage.en.srt", "srt", "eng", "With.Additional.Garbage", index),
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles_TestData))]
        public void AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles(List<MediaStream> streams, string videoPath, int startIndex, string[] files, MediaStream[] expectedResult)
        {
            new SubtitleResolver(_localizationManager).AddExternalSubtitleStreams(streams, videoPath, startIndex, files);

            Assert.Equal(expectedResult.Length, streams.Count);
            for (var i = 0; i < expectedResult.Length; i++)
            {
                var expected = expectedResult[i];
                var actual = streams[i];

                Assert.Equal(expected.Index, actual.Index);
                Assert.Equal(expected.Codec, actual.Codec);
                Assert.Equal(expected.Type, actual.Type);
                Assert.Equal(expected.IsExternal, actual.IsExternal);
                Assert.Equal(expected.Path, actual.Path);
                Assert.Equal(expected.IsDefault, actual.IsDefault);
                Assert.Equal(expected.IsForced, actual.IsForced);
                Assert.Equal(expected.Language, actual.Language);
                Assert.Equal(expected.Title, actual.Title);
            }
        }

        [Theory]
        [InlineData("/video/My Video.mkv", "/video/My Video.srt", "srt", null, null, false, false)]
        [InlineData("/video/My Video.mkv", "/video/My Video.ass", "ass", null, null, false, false)]
        [InlineData("/video/My Video.mkv", "/video/my video.srt", "srt", null, null, false, false)]
        [InlineData("/video/My Video.mkv", "/video/My Vidèo.srt", "srt", null, null, false, false)]
        [InlineData("/video/My_Video.mkv", "/video/My. Video.srt", "srt", null, null, false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.srt", "srt", null, null, false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.foreign.srt", "srt", null, null, true, false)]
        [InlineData("/video/My Video.mkv", "/video/My Video.forced.srt", "srt", null, null, true, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.srt", "srt", null, null, false, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.forced.default.srt", "srt", null, null, true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.srt", "srt", "eng", null, false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.fr.en.srt", "srt", "eng", "fr", false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.fr.srt", "srt", "fre", "en", false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.en.srt", "srt", "eng", null, false, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.default.forced.en.srt", "srt", "eng", null, true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.default.forced.srt", "srt", "eng", null, true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.Track Label.srt", "srt", null, "Track Label", false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.Track.Label.srt", "srt", null, "Track.Label", false, false)]
        [InlineData("/video/MyVideo.mkv", "/video/My.Video.Track Label.srt", "srt", null, "Track Label", false, false)]
        [InlineData("/video/My _ Video.mkv", "/video/MyVideo.Track Label.srt", "srt", null, "Track Label", false, false)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.Track Label.en.default.forced.srt", "srt", "eng", "Track Label", true, true)]
        [InlineData("/video/My.Video.mkv", "/video/My.Video.en.default.forced.Track Label.srt", "srt", "eng", "Track Label", true, true)]
        public void AddExternalSubtitleStreams_GivenSingleFile_ReturnsExpectedSubtitle(string videoPath, string file, string codec, string? language, string? title, bool isForced, bool isDefault)
        {
            var streams = new List<MediaStream>();
            new SubtitleResolver(_localizationManager).AddExternalSubtitleStreams(streams, videoPath, 0, new[] { file });

            Assert.Single(streams);
            var actual = streams[0];

            var expected = CreateMediaStream(file, codec, language, title, 0, isForced, isDefault);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Codec, actual.Codec);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.IsExternal, actual.IsExternal);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.IsDefault, actual.IsDefault);
            Assert.Equal(expected.IsForced, actual.IsForced);
            Assert.Equal(expected.Language, actual.Language);
            Assert.Equal(expected.Title, actual.Title);
        }

        private static MediaStream CreateMediaStream(string path, string codec, string? language, string? title, int index, bool isForced = false, bool isDefault = false)
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
                Language = language,
                Title = title
            };
        }
    }
}
