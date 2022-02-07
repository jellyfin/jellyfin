using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class SubtitleResolverTests
    {
        private const string VideoDirectoryPath = "Test Data/Video";
        private const string MetadataDirectoryPath = "Test Data/Metadata";
        private readonly MediaInfoResolver _subtitleResolver;

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

            _subtitleResolver = new MediaInfoResolver(localizationManager.Object, mediaEncoder.Object, new NamingOptions(), DlnaProfileType.Subtitle);
        }

        [Fact]
        public async void AddExternalSubtitleStreams_GivenMixedFilenames_ReturnsValidSubtitles()
        {
            var startIndex = 0;
            var index = startIndex;
            var files = new[]
            {
                VideoDirectoryPath + "/MyVideo.en.srt",
                VideoDirectoryPath + "/MyVideo.en.forced.default.sub",
                VideoDirectoryPath + "/My.Video.mp3",
                VideoDirectoryPath + "/My.Video.png",
                VideoDirectoryPath + "/My.Video.srt",
                VideoDirectoryPath + "/My.Video.txt",
                VideoDirectoryPath + "/My.Video.vtt",
                VideoDirectoryPath + "/My.Video.ass",
                VideoDirectoryPath + "/My.Video.sub",
                VideoDirectoryPath + "/My.Video.ssa",
                VideoDirectoryPath + "/My.Video.smi",
                VideoDirectoryPath + "/My.Video.sami",
                VideoDirectoryPath + "/My.Video.mks",
                VideoDirectoryPath + "/My.Video.en.srt",
                VideoDirectoryPath + "/My.Video.default.en.srt",
                VideoDirectoryPath + "/My.Video.default.forced.en.srt",
                VideoDirectoryPath + "/My.Video.en.default.forced.srt",
                VideoDirectoryPath + "/My.Video.en.With Additional Garbage.sub",
                VideoDirectoryPath + "/My.Video.With Additional Garbage.English.sub",
                VideoDirectoryPath + "/My.Video.With.Additional.Garbage.en.srt",
                VideoDirectoryPath + "/Some.Other.Video.srt"
            };
            var metadataFiles = new[]
            {
                MetadataDirectoryPath + "/My.Video.en.srt"
            };
            var expectedResult = new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/MyVideo.en.srt", "srt", "eng", null, index++),
                CreateMediaStream(VideoDirectoryPath + "/MyVideo.en.forced.default.sub", "sub", "eng", null, index++, isDefault: true, isForced: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.srt", "srt", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.vtt", "vtt", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.ass", "ass", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.sub", "sub", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.ssa", "ssa", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.smi", "smi", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.sami", "sami", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.mks", "mks", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.srt", "srt", "eng", null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.default.en.srt", "srt", "eng", null, index++, isDefault: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.default.forced.en.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.default.forced.srt", "srt", "eng", null, index++, isForced: true, isDefault: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.With Additional Garbage.sub", "sub", "eng", "With Additional Garbage", index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.With Additional Garbage.English.sub", "sub", "eng", "With Additional Garbage", index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.With.Additional.Garbage.en.srt", "srt", "eng", "With.Additional.Garbage", index++),
                CreateMediaStream(MetadataDirectoryPath + "/My.Video.en.srt", "srt", "eng", null, index)
            };

            BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

            var video = new Mock<Video>();
            video.CallBase = true;
            video.Setup(moq => moq.Path).Returns(VideoDirectoryPath + "/My.Video.mkv");
            video.Setup(moq => moq.GetInternalMetadataPath()).Returns(MetadataDirectoryPath);

            var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Video"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(files);
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Metadata"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(metadataFiles);

            var asyncStreams = _subtitleResolver.GetExternalStreamsAsync(video.Object, startIndex, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

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
                Assert.Equal(expected.IsDefault, actual.IsDefault);
                Assert.Equal(expected.IsForced, actual.IsForced);
                Assert.Equal(expected.Language, actual.Language);
                Assert.Equal(expected.Title, actual.Title);
            }
        }

        [Theory]
        [InlineData("MyVideo.en.srt", "srt", "eng", null, false, false)]
        [InlineData("MyVideo.en.forced.default.srt", "srt", "eng", null, true, true)]
        [InlineData("My.Video.srt", "srt", null, null, false, false)]
        [InlineData("My.Video.foreign.srt", "srt", null, null, true, false)]
        [InlineData("My.Video.default.srt", "srt", null, null, false, true)]
        [InlineData("My.Video.forced.default.srt", "srt", null, null, true, true)]
        [InlineData("My.Video.en.srt", "srt", "eng", null, false, false)]
        [InlineData("My.Video.fr.en.srt", "srt", "eng", "fr", false, false)]
        [InlineData("My.Video.en.fr.srt", "srt", "fre", "en", false, false)]
        [InlineData("My.Video.default.en.srt", "srt", "eng", null, false, true)]
        [InlineData("My.Video.default.forced.en.srt", "srt", "eng", null, true, true)]
        [InlineData("My.Video.en.default.forced.srt", "srt", "eng", null, true, true)]
        [InlineData("My.Video.Track Label.srt", "srt", null, "Track Label", false, false)]
        [InlineData("My.Video.Track.Label.srt", "srt", null, "Track.Label", false, false)]
        [InlineData("My.Video.Track Label.en.default.forced.srt", "srt", "eng", "Track Label", true, true)]
        [InlineData("My.Video.en.default.forced.Track Label.srt", "srt", "eng", "Track Label", true, true)]
        public async void AddExternalSubtitleStreams_GivenSingleFile_ReturnsExpectedSubtitle(string file, string codec, string? language, string? title, bool isForced, bool isDefault)
        {
            BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

            var video = new Mock<Video>();
            video.CallBase = true;
            video.Setup(moq => moq.Path).Returns(VideoDirectoryPath + "/My.Video.mkv");
            video.Setup(moq => moq.GetInternalMetadataPath()).Returns(MetadataDirectoryPath);

            var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Video"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new[] { VideoDirectoryPath + "/" + file });
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(@"Test Data[/\\]Metadata"), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());

            var asyncStreams = _subtitleResolver.GetExternalStreamsAsync(video.Object, 0, directoryService.Object, false, CancellationToken.None).ConfigureAwait(false);

            var streams = new List<MediaStream>();
            await foreach (var stream in asyncStreams)
            {
                streams.Add(stream);
            }

            Assert.Single(streams);
            var actual = streams[0];

            var expected = CreateMediaStream(VideoDirectoryPath + "/" + file, codec, language, title, 0, isForced, isDefault);
            Assert.Equal(expected.Index, actual.Index);
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
