using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class SubtitleResolverTests
    {
        private const string DirectoryPath = "Test Data/Video";
        private readonly SubtitleResolver _subtitleResolver;

        public SubtitleResolverTests()
        {
            var englishCultureDto = new CultureDto
            {
                Name = "English",
                DisplayName = "English",
                ThreeLetterISOLanguageNames = new[] { "eng" },
                TwoLetterISOLanguageName = "en"
            };
            var frenchCultureDto = new CultureDto
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

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(me => me.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
                .Returns<MediaInfoRequest, CancellationToken>((_, _) => Task.FromResult(new MediaBrowser.Model.MediaInfo.MediaInfo
                {
                    MediaStreams = new List<MediaStream>
                    {
                        new()
                    }
                }));

            _subtitleResolver = new SubtitleResolver(localizationManager.Object, mediaEncoder.Object, new NamingOptions());
        }

        [Fact]
        public async void AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles()
        {
            var startIndex = 0;
            var index = startIndex;
            var files = new[]
            {
                DirectoryPath + "/My.Video.mp3",
                DirectoryPath + "/My.Video.png",
                DirectoryPath + "/My.Video.srt",
                // DirectoryPath + "/Some.Other.Video.srt", // TODO should not be picked up
                DirectoryPath + "/My.Video.txt",
                DirectoryPath + "/My.Video.vtt",
                DirectoryPath + "/My.Video.ass",
                DirectoryPath + "/My.Video.sub",
                DirectoryPath + "/My.Video.ssa",
                DirectoryPath + "/My.Video.smi",
                DirectoryPath + "/My.Video.sami",
                DirectoryPath + "/My.Video.en.srt",
                DirectoryPath + "/My.Video.default.en.srt",
                DirectoryPath + "/My.Video.default.forced.en.srt",
                DirectoryPath + "/My.Video.en.default.forced.srt",
                DirectoryPath + "/My.Video.With.Additional.Garbage.en.srt",
                // DirectoryPath + "/My.Video With Additional Garbage.srt" // TODO no "." after "My.Video", previously would be picked up
            };
            var expectedResult = new[]
            {
                CreateMediaStream(DirectoryPath + "/My.Video.srt", "srt", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.vtt", "vtt", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.ass", "ass", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.sub", "sub", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.ssa", "ssa", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.smi", "smi", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.sami", "sami", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.en.srt", "srt", "eng", null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.default.en.srt", "srt", "eng", null, index++, isDefault: true),
                CreateMediaStream(DirectoryPath + "/My.Video.default.forced.en.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                CreateMediaStream(DirectoryPath + "/My.Video.en.default.forced.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                CreateMediaStream(DirectoryPath + "/My.Video.With.Additional.Garbage.en.srt", "srt", "eng", "Garbage", index) // TODO only "Garbage" is picked up as title, none of the other extra text
            };

            BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();
            var video = new Movie
            {
                // Must be valid for video.IsFileProtocol check
                Path = DirectoryPath + "/My.Video.mkv"
            };

            var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Video"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(files);

            var asyncStreams = _subtitleResolver.GetExternalSubtitleStreams(video, startIndex, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

            var streams = new List<MediaStream>();
            await foreach (var stream in asyncStreams)
            {
                streams.Add(stream);
            }

            Assert.Equal(expectedResult.Length, streams.Count);
            for (var i = 0; i < expectedResult.Length; i++)
            {
                var expected = expectedResult[i];
                var actual = streams[i];

                Assert.Equal(expected.Index, actual.Index);
                // Assert.Equal(expected.Codec, actual.Codec); TODO should codec still be set to file extension?
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
        [InlineData("My Video.srt", "srt", null, null, false, false)]
        [InlineData("My Video.ass", "ass", null, null, false, false)]
        [InlineData("my video.srt", "srt", null, null, false, false)]
        [InlineData("My Vidèo.srt", "srt", null, null, false, false)]
        [InlineData("My. Video.srt", "srt", null, null, false, false)]
        [InlineData("My.Video.srt", "srt", null, null, false, false)]
        [InlineData("My.Video.foreign.srt", "srt", null, null, true, false)]
        [InlineData("My Video.forced.srt", "srt", null, null, true, false)]
        [InlineData("My.Video.default.srt", "srt", null, null, false, true)]
        [InlineData("My.Video.forced.default.srt", "srt", null, null, true, true)]
        [InlineData("My.Video.en.srt", "srt", "eng", null, false, false)]
        [InlineData("My.Video.fr.en.srt", "srt", "eng", "fr", false, false)]
        [InlineData("My.Video.en.fr.srt", "srt", "fre", "en", false, false)]
        [InlineData("My.Video.default.en.srt", "srt", "eng", null, false, true)]
        [InlineData("My.Video.default.forced.en.srt", "srt", "eng", null, true, true)]
        [InlineData("My.Video.en.default.forced.srt", "srt", "eng", null, true, true)]
        [InlineData("My.Video.Track Label.srt", "srt", null, "Track Label", false, false)]
        // [InlineData("My.Video.Track.Label.srt", "srt", null, "Track.Label", false, false)] // TODO fails - only "Label" is picked up for title, not "Track.Label"
        // [InlineData("MyVideo.Track Label.srt", "srt", null, "Track Label", false, false)] // TODO fails - fuzzy match doesn't pick up on end of matching segment being shorter?
        [InlineData("My.Video.Track Label.en.default.forced.srt", "srt", "eng", "Track Label", true, true)]
        [InlineData("My.Video.en.default.forced.Track Label.srt", "srt", "eng", "Track Label", true, true)]
        public async void AddExternalSubtitleStreams_GivenSingleFile_ReturnsExpectedSubtitle(string file, string codec, string? language, string? title, bool isForced, bool isDefault)
        {
            BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();
            var video = new Movie
            {
                // Must be valid for video.IsFileProtocol check
                Path = DirectoryPath + "/My.Video.mkv"
            };

            var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Video"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new[] { DirectoryPath + "/" + file });

            var asyncStreams = _subtitleResolver.GetExternalSubtitleStreams(video, 0, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

            var streams = new List<MediaStream>();
            await foreach (var stream in asyncStreams)
            {
                streams.Add(stream);
            }

            Assert.Single(streams);
            var actual = streams[0];

            var expected = CreateMediaStream(DirectoryPath + "/" + file, codec, language, title, 0, isForced, isDefault);
            Assert.Equal(expected.Index, actual.Index);
            // Assert.Equal(expected.Codec, actual.Codec); TODO should codec still be set to file extension?
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
