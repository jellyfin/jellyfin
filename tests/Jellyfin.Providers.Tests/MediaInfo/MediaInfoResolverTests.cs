using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class MediaInfoResolverTests
{
    public const string VideoDirectoryPath = "Test Data/Video";
    public const string VideoDirectoryRegex = @"Test Data[/\\]Video";
    public const string MetadataDirectoryPath = "library/00/00000000000000000000000000000000";
    public const string MetadataDirectoryRegex = "library.*";

    private readonly ILocalizationManager _localizationManager;
    private readonly MediaInfoResolver _subtitleResolver;

    public MediaInfoResolverTests()
    {
        // prep BaseItem and Video for calls made that expect managers
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();

        var applicationPaths = new Mock<IServerApplicationPaths>().Object;
        var serverConfig = new Mock<IServerConfigurationManager>();
        serverConfig.Setup(c => c.ApplicationPaths)
            .Returns(applicationPaths);
        BaseItem.ConfigurationManager = serverConfig.Object;

        // build resolver to test with
        var englishCultureDto = new CultureDto("English", "English", "en", new[] { "eng" });

        var localizationManager = new Mock<ILocalizationManager>(MockBehavior.Loose);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex("en.*", RegexOptions.IgnoreCase)))
            .Returns(englishCultureDto);
        _localizationManager = localizationManager.Object;

        var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
        mediaEncoder.Setup(me => me.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MediaInfoRequest, CancellationToken>((_, _) => Task.FromResult(new MediaBrowser.Model.MediaInfo.MediaInfo
            {
                MediaStreams = new List<MediaStream>
                {
                    new()
                }
            }));

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>()))
            .Returns(false);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(VideoDirectoryRegex)))
            .Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(MetadataDirectoryRegex)))
            .Returns(true);

        _subtitleResolver = new SubtitleResolver(Mock.Of<ILogger<SubtitleResolver>>(), _localizationManager, mediaEncoder.Object, fileSystem.Object, new NamingOptions());
    }

    [Fact]
    public void GetExternalFiles_BadProtocol_ReturnsNoSubtitles()
    {
        // need a media source manager capable of returning something other than file protocol
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsRegex("http.*")))
            .Returns(MediaProtocol.Http);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        var video = new Movie
        {
            Path = "https://url.com/My.Video.mkv"
        };

        Assert.Empty(_subtitleResolver.GetExternalFiles(video, Mock.Of<IDirectoryService>(), false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetExternalFiles_MissingDirectory_DirectoryNotQueried(bool metadataDirectory)
    {
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        string containingFolderPath, metadataPath;

        if (metadataDirectory)
        {
            containingFolderPath = VideoDirectoryPath;
            metadataPath = "invalid";
        }
        else
        {
            containingFolderPath = "invalid";
            metadataPath = MetadataDirectoryPath;
        }

        var video = new Mock<Movie>();
        video.Setup(m => m.Path)
            .Returns(VideoDirectoryPath + "/My.Video.mkv");
        video.Setup(m => m.ContainingFolderPath)
            .Returns(containingFolderPath);
        video.Setup(m => m.GetInternalMetadataPath())
            .Returns(metadataPath);

        string pathNotFoundRegex = metadataDirectory ? MetadataDirectoryRegex : VideoDirectoryRegex;

        var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
        // any path other than test target exists and provides an empty listing
        directoryService.Setup(ds => ds.GetFilePaths(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Array.Empty<string>());

        _subtitleResolver.GetExternalFiles(video.Object, directoryService.Object, false);

        directoryService.Verify(
            ds => ds.GetFilePaths(It.IsRegex(pathNotFoundRegex), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Theory]
    [InlineData("My.Video.mkv", "My.Video.srt", null)]
    [InlineData("My.Video.mkv", "My.Video.en.srt", "eng")]
    [InlineData("My.Video.mkv", "My.Video.en.srt", "eng", true)]
    [InlineData("Example Movie (2021).mp4", "Example Movie (2021).English.Srt", "eng")]
    [InlineData("[LTDB] Who Framed Roger Rabbit (1998) - [Bluray-1080p].mkv", "[LTDB] Who Framed Roger Rabbit (1998) - [Bluray-1080p].en.srt", "eng")]
    public void GetExternalFiles_NameMatching_MatchesAndParsesToken(string movie, string file, string? language, bool metadataDirectory = false)
    {
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        var video = new Movie
        {
            Path = VideoDirectoryPath + "/" + movie
        };

        var directoryService = GetDirectoryServiceForExternalFile(file, metadataDirectory);
        var streams = _subtitleResolver.GetExternalFiles(video, directoryService, false).ToList();

        Assert.Single(streams);
        var actual = streams[0];
        Assert.Equal(language, actual.Language);
        Assert.Null(actual.Title);
    }

    [Theory]
    [InlineData("cover.jpg")]
    [InlineData("My.Video.mp3")]
    [InlineData("My.Video.png")]
    [InlineData("My.Video.txt")]
    [InlineData("My.Video Sequel.srt")]
    [InlineData("Some.Other.Video.srt")]
    public void GetExternalFiles_NameMatching_RejectsNonMatches(string file)
    {
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        var video = new Movie
        {
            Path = VideoDirectoryPath + "/My.Video.mkv"
        };

        var directoryService = GetDirectoryServiceForExternalFile(file);
        var streams = _subtitleResolver.GetExternalFiles(video, directoryService, false).ToList();

        Assert.Empty(streams);
    }

    [Theory]
    [InlineData("https://url.com/My.Video.mkv")]
    [InlineData(VideoDirectoryPath)] // valid but no files found for this test
    public async Task GetExternalStreams_BadPaths_ReturnsNoSubtitles(string path)
    {
        // need a media source manager capable of returning something other than file protocol
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsRegex("http.*")))
            .Returns(MediaProtocol.Http);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        var video = new Movie
        {
            Path = path
        };

        var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
        directoryService.Setup(ds => ds.GetFilePaths(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Array.Empty<string>());

        var mediaEncoder = Mock.Of<IMediaEncoder>(MockBehavior.Strict);
        var fileSystem = Mock.Of<IFileSystem>();

        var subtitleResolver = new SubtitleResolver(Mock.Of<ILogger<SubtitleResolver>>(), _localizationManager, mediaEncoder, fileSystem, new NamingOptions());

        var streams = await subtitleResolver.GetExternalStreamsAsync(video, 0, directoryService.Object, false, CancellationToken.None);

        Assert.Empty(streams);
    }

    public static TheoryData<string, MediaStream[], MediaStream[]> GetExternalStreams_MergeMetadata_HandlesOverridesCorrectly_Data()
    {
        var data = new TheoryData<string, MediaStream[], MediaStream[]>();

        // filename and stream have no metadata set
        string file = "My.Video.srt";
        data.Add(
            file,
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, null, null, 0)
            },
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, null, null, 0)
            });

        // filename has metadata
        file = "My.Video.Title1.default.forced.sdh.en.srt";
        data.Add(
            file,
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, null, null, 0)
            },
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "eng", "Title1", 0, true, true, true)
            });

        // single stream with metadata
        file = "My.Video.mks";
        data.Add(
            file,
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "eng", "Title", 0, true, true, true)
            },
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "eng", "Title", 0, true, true, true)
            });

        // stream wins for title/language, filename wins for flags when conflicting
        file = "My.Video.Title2.default.forced.sdh.en.srt";
        data.Add(
            file,
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "fra", "Metadata", 0)
            },
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "fra", "Metadata", 0, true, true, true)
            });

        // multiple stream with metadata - filename flags ignored but other data filled in when missing from stream
        file = "My.Video.Title3.default.forced.en.srt";
        data.Add(
            file,
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, null, null, 0, true, true),
                CreateMediaStream(VideoDirectoryPath + "/" + file, "fra", "Metadata", 1)
            },
            new[]
            {
                CreateMediaStream(VideoDirectoryPath + "/" + file, "eng", "Title3", 0, true, true),
                CreateMediaStream(VideoDirectoryPath + "/" + file, "fra", "Metadata", 1)
            });

        return data;
    }

    [Theory]
    [MemberData(nameof(GetExternalStreams_MergeMetadata_HandlesOverridesCorrectly_Data))]
    public async Task GetExternalStreams_MergeMetadata_HandlesOverridesCorrectly(string file, MediaStream[] inputStreams, MediaStream[] expectedStreams)
    {
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        var video = new Movie
        {
            Path = VideoDirectoryPath + "/My.Video.mkv"
        };

        var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
        mediaEncoder.Setup(me => me.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MediaInfoRequest, CancellationToken>((_, _) => Task.FromResult(new MediaBrowser.Model.MediaInfo.MediaInfo
            {
                MediaStreams = inputStreams.ToList()
            }));

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(VideoDirectoryRegex)))
            .Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(MetadataDirectoryRegex)))
            .Returns(true);

        var subtitleResolver = new SubtitleResolver(Mock.Of<ILogger<SubtitleResolver>>(), _localizationManager, mediaEncoder.Object, fileSystem.Object, new NamingOptions());

        var directoryService = GetDirectoryServiceForExternalFile(file);
        var streams = await subtitleResolver.GetExternalStreamsAsync(video, 0, directoryService, false, CancellationToken.None);

        Assert.Equal(expectedStreams.Length, streams.Count);
        for (var i = 0; i < expectedStreams.Length; i++)
        {
            var expected = expectedStreams[i];
            var actual = streams[i];

            Assert.True(actual.IsExternal);
            Assert.Equal(expected.Index, actual.Index);
            Assert.Equal(expected.Type, actual.Type);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.IsDefault, actual.IsDefault);
            Assert.Equal(expected.IsForced, actual.IsForced);
            Assert.Equal(expected.IsHearingImpaired, actual.IsHearingImpaired);
            Assert.Equal(expected.Language, actual.Language);
            Assert.Equal(expected.Title, actual.Title);
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    public async Task GetExternalStreams_StreamIndex_HandlesFilesAndContainers(int fileCount, int streamCount)
    {
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        var video = new Movie
        {
            Path = VideoDirectoryPath + "/My.Video.mkv"
        };

        var files = new string[fileCount];
        for (int i = 0; i < fileCount; i++)
        {
            files[i] = $"{VideoDirectoryPath}/My.Video.{i}.srt";
        }

        var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
        directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(VideoDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(files);
        directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(MetadataDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(Array.Empty<string>());

        List<MediaStream> GenerateMediaStreams()
        {
            var mediaStreams = new List<MediaStream>();
            for (int i = 0; i < streamCount; i++)
            {
                mediaStreams.Add(new()
                {
                    Type = MediaStreamType.Subtitle
                });
            }

            return mediaStreams;
        }

        var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
        mediaEncoder.Setup(me => me.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MediaInfoRequest, CancellationToken>((_, _) => Task.FromResult(new MediaBrowser.Model.MediaInfo.MediaInfo
            {
                MediaStreams = GenerateMediaStreams()
            }));

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(VideoDirectoryRegex)))
            .Returns(true);
        fileSystem.Setup(fs => fs.DirectoryExists(It.IsRegex(MetadataDirectoryRegex)))
            .Returns(true);

        var subtitleResolver = new SubtitleResolver(Mock.Of<ILogger<SubtitleResolver>>(), _localizationManager, mediaEncoder.Object, fileSystem.Object, new NamingOptions());

        int startIndex = 1;
        var streams = await subtitleResolver.GetExternalStreamsAsync(video, startIndex, directoryService.Object, false, CancellationToken.None);

        Assert.Equal(fileCount * streamCount, streams.Count);
        for (var i = 0; i < streams.Count; i++)
        {
            Assert.Equal(startIndex + i, streams[i].Index);
            // intentional integer division to ensure correct number of streams come back from each file
            Assert.Matches(@$".*\.{i / streamCount}\.srt", streams[i].Path);
        }
    }

    private static MediaStream CreateMediaStream(string path, string? language, string? title, int index, bool isForced = false, bool isDefault = false, bool isHearingImpaired = false)
    {
        return new MediaStream
        {
            Index = index,
            Type = MediaStreamType.Subtitle,
            Path = path,
            IsDefault = isDefault,
            IsForced = isForced,
            IsHearingImpaired = isHearingImpaired,
            Language = language,
            Title = title
        };
    }

    /// <summary>
    /// Provides an <see cref="IDirectoryService"/> that when queried for the test video/metadata directory will return a path including the provided file name.
    /// </summary>
    /// <param name="file">The name of the file to locate.</param>
    /// <param name="useMetadataDirectory"><c>true</c> if the file belongs in the metadata directory.</param>
    /// <returns>A mocked <see cref="IDirectoryService"/>.</returns>
    public static IDirectoryService GetDirectoryServiceForExternalFile(string file, bool useMetadataDirectory = false)
    {
        var directoryService = new Mock<IDirectoryService>(MockBehavior.Strict);
        if (useMetadataDirectory)
        {
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(VideoDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(MetadataDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new[] { MetadataDirectoryPath + "/" + file });
        }
        else
        {
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(VideoDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new[] { VideoDirectoryPath + "/" + file });
            directoryService.Setup(ds => ds.GetFilePaths(It.IsRegex(MetadataDirectoryRegex), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(Array.Empty<string>());
        }

        return directoryService.Object;
    }
}
