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
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class AudioResolverTests
    {
        private const string VideoDirectoryPath = "Test Data/Video";
        private const string MetadataDirectoryPath = "Test Data/Metadata";
        private readonly AudioResolver _audioResolver;

        public AudioResolverTests()
        {
            var englishCultureDto = new CultureDto("English", "English", "en", new[] { "eng" });

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
        public async void AddExternalStreamsAsync_GivenMixedFilenames_ReturnsValidSubtitles()
        {
            var startIndex = 0;
            var index = startIndex;
            var files = new[]
            {
                VideoDirectoryPath + "/MyVideo.en.aac",
                VideoDirectoryPath + "/MyVideo.en.forced.default.dts",
                VideoDirectoryPath + "/My.Video.mp3",
                VideoDirectoryPath + "/Some.Other.Video.mp3",
                VideoDirectoryPath + "/My.Video.png",
                VideoDirectoryPath + "/My.Video.srt",
                VideoDirectoryPath + "/My.Video.txt",
                VideoDirectoryPath + "/My.Video.vtt",
                VideoDirectoryPath + "/My.Video.ass",
                VideoDirectoryPath + "/My.Video.sub",
                VideoDirectoryPath + "/My.Video.ssa",
                VideoDirectoryPath + "/My.Video.smi",
                VideoDirectoryPath + "/My.Video.sami",
                VideoDirectoryPath + "/My.Video.en.mp3",
                VideoDirectoryPath + "/My.Video.en.forced.mp3",
                VideoDirectoryPath + "/My.Video.en.default.forced.aac",
                VideoDirectoryPath + "/My.Video.Label.mp3",
                VideoDirectoryPath + "/My.Video.With Additional Garbage.en.aac",
                VideoDirectoryPath + "/My.Video.With.Additional.Garbage.en.mp3"
            };
            var metadataFiles = new[]
            {
                MetadataDirectoryPath + "/My.Video.en.aac"
            };
            var expectedResult = new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/MyVideo.en.aac", "eng", null, index++),
                CreateMediaStream(VideoDirectoryPath + "/MyVideo.en.forced.default.dts", "eng", null, index++, isDefault: true, isForced: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.mp3", null, null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.mp3", "eng", null, index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.forced.mp3", "eng", null, index++, isDefault: false, isForced: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.en.default.forced.aac", "eng", null, index++, isDefault: true, isForced: true),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.Label.mp3", null, "Label", index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.With Additional Garbage.en.aac", "eng", "With Additional Garbage", index++),
                CreateMediaStream(VideoDirectoryPath + "/My.Video.With.Additional.Garbage.en.mp3", "eng", "With.Additional.Garbage", index++),
                CreateMediaStream(MetadataDirectoryPath + "/My.Video.en.aac", "eng", null, index)
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

            var streams = await _audioResolver.GetExternalStreamsAsync(video.Object, startIndex, directoryService.Object, false, CancellationToken.None);

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
        [InlineData("MyVideo.en.aac", "eng", null, false, false)]
        [InlineData("MyVideo.en.forced.default.dts", "eng", null, true, true)]
        [InlineData("My.Video.mp3", null, null, false, false)]
        [InlineData("My.Video.English.mp3", "eng", null, false, false)]
        [InlineData("My.Video.Title.mp3", null, "Title", false, false)]
        [InlineData("My.Video.forced.English.mp3", "eng", null, true, false)]
        [InlineData("My.Video.default.English.mp3", "eng", null, false, true)]
        [InlineData("My.Video.English.forced.default.Title.mp3", "eng", "Title", true, true)]
        public async void AddExternalStreamsAsync_GivenSingleFile_ReturnsExpectedStream(string file, string? language, string? title, bool isForced, bool isDefault)
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

            var streams = await _audioResolver.GetExternalStreamsAsync(video.Object, 0, directoryService.Object, false, CancellationToken.None);

            Assert.Single(streams);

            var actual = streams[0];

            var expected = CreateMediaStream(VideoDirectoryPath + "/" + file, language, title, 0, isForced, isDefault);
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
