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
    public class AudioResolverTests
    {
        private const string DirectoryPath = "Test Data/Video";
        private readonly AudioResolver _audioResolver;

        public AudioResolverTests()
        {
            var englishCultureDto = new CultureDto
            {
                Name = "English",
                DisplayName = "English",
                ThreeLetterISOLanguageNames = new[] { "eng" },
                TwoLetterISOLanguageName = "en"
            };

            var localizationManager = new Mock<ILocalizationManager>(MockBehavior.Loose);
            localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex(@"en.*", RegexOptions.IgnoreCase)))
                .Returns(englishCultureDto);

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(me => me.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
                .Returns<MediaInfoRequest, CancellationToken>((_, _) => Task.FromResult(new MediaBrowser.Model.MediaInfo.MediaInfo
                {
                    MediaStreams = new List<MediaStream>
                    {
                        new()
                    }
                }));

            _audioResolver = new AudioResolver(localizationManager.Object, mediaEncoder.Object, new NamingOptions());
        }

        [Fact]
        public async void AddExternalAudioStreams_GivenMixedFilenames_ReturnsValidSubtitles()
        {
            var startIndex = 0;
            var index = startIndex;
            var files = new[]
            {
                DirectoryPath + "/My.Video.mp3",
                // DirectoryPath + "/Some.Other.Video.mp3", // TODO should not be picked up
                DirectoryPath + "/My.Video.png",
                DirectoryPath + "/My.Video.srt",
                DirectoryPath + "/My.Video.txt",
                DirectoryPath + "/My.Video.vtt",
                DirectoryPath + "/My.Video.ass",
                DirectoryPath + "/My.Video.sub",
                DirectoryPath + "/My.Video.ssa",
                DirectoryPath + "/My.Video.smi",
                DirectoryPath + "/My.Video.sami",
                DirectoryPath + "/My.Video.en.mp3",
                DirectoryPath + "/My.Video.Label.mp3",
                DirectoryPath + "/My.Video.With.Additional.Garbage.en.mp3",
                // DirectoryPath + "/My.Video With Additional Garbage.mp3" // TODO no "." after "My.Video", previously would be picked up
            };
            var expectedResult = new[]
            {
                CreateMediaStream(DirectoryPath + "/My.Video.mp3", null, null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.en.mp3", "eng", null, index++),
                CreateMediaStream(DirectoryPath + "/My.Video.Label.mp3", null, "Label", index++),
                CreateMediaStream(DirectoryPath + "/My.Video.With.Additional.Garbage.en.mp3", "eng", "Garbage", index) // TODO only "Garbage" is picked up as title, none of the other extra text
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

            var asyncStreams = _audioResolver.GetExternalAudioStreams(video, startIndex, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

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
                Assert.Equal(expected.Type, actual.Type);
                Assert.Equal(expected.IsExternal, actual.IsExternal);
                Assert.Equal(expected.Path, actual.Path);
                Assert.Equal(expected.Language, actual.Language);
                Assert.Equal(expected.Title, actual.Title);
            }
        }

        [Theory]
        [InlineData("My.Video.mp3", null, null, false, false)]
        [InlineData("My.Video.English.mp3", "eng", null, false, false)]
        [InlineData("My.Video.Title.mp3", null, "Title", false, false)]
        [InlineData("My.Video.forced.English.mp3", "eng", null, true, false)]
        [InlineData("My.Video.default.English.mp3", "eng", null, false, true)]
        [InlineData("My.Video.English.forced.default.Title.mp3", "eng", "Title", true, true)]
        public async void GetExternalAudioStreams_GivenSingleFile_ReturnsExpectedStream(string file, string? language, string? title, bool isForced, bool isDefault)
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

            var asyncStreams = _audioResolver.GetExternalAudioStreams(video, 0, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

            var streams = new List<MediaStream>();
            await foreach (var stream in asyncStreams)
            {
                streams.Add(stream);
            }

            Assert.Single(streams);

            var actual = streams[0];

            var expected = CreateMediaStream(DirectoryPath + "/" + file, language, title, 0, isForced, isDefault);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.IsExternal, actual.IsExternal);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.Language, actual.Language);
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.IsDefault, actual.IsDefault);
            Assert.Equal(expected.IsForced, actual.IsForced);
        }

        private static MediaStream CreateMediaStream(string path, string? language, string? title, int index, bool isForced = false, bool isDefault = false)
        {
            return new()
            {
                Index = index,
                Type = MediaStreamType.Audio,
                IsExternal = true,
                Path = path,
                Language = language,
                Title = title,
                IsForced = isForced,
                IsDefault = isDefault
            };
        }
    }
}
