using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class ProbeProviderTests
{
    private const string ShortcutTarget = "https://example.com/part2.mp4";

    private readonly Mock<IMediaEncoder> _mediaEncoder = new();
    private readonly ProbeProvider _probeProvider;

    public ProbeProviderTests()
    {
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);
        mediaSourceManager.Setup(m => m.GetPathProtocol(ShortcutTarget))
            .Returns(MediaProtocol.Http);

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions());

        // prep BaseItem and Video for calls made that expect managers
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();

        _probeProvider = new ProbeProvider(
            mediaSourceManager.Object,
            _mediaEncoder.Object,
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IChapterManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ISubtitleManager>(),
            libraryManager.Object,
            Mock.Of<IFileSystem>(),
            NullLoggerFactory.Instance,
            new NamingOptions(),
            Mock.Of<ILyricManager>(),
            Mock.Of<IMediaAttachmentRepository>(),
            Mock.Of<IMediaStreamRepository>());
    }

    [Fact]
    public void HasChanged_NeverProbedVideo_ReturnsTrue()
    {
        // A probe that threw leaves the item like this while the refresh is stamped as done, and the
        // file's modification time never changes afterwards, so nothing else would ask for a retry.
        var item = new Episode { Path = "/media/show/S01E01.mkv" };

        Assert.True(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_NeverProbedAudio_ReturnsTrue()
    {
        var item = new Audio { Path = "/media/music/track.flac" };

        Assert.True(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Theory]
    [InlineData(12345L, null)]
    [InlineData(null, 2480000)]
    [InlineData(12345L, 2480000)]
    public void HasChanged_ProbedVideo_ReturnsFalse(long? runTimeTicks, int? totalBitrate)
    {
        var item = new Episode
        {
            Path = "/media/show/S01E01.mkv",
            RunTimeTicks = runTimeTicks,
            TotalBitrate = totalBitrate
        };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_VirtualItemWithoutMediaInfo_ReturnsFalse()
    {
        var item = new Episode { Path = "/media/show/S01E01.mkv", IsVirtualItem = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_PlaceHolderWithoutMediaInfo_ReturnsFalse()
    {
        var item = new Episode { Path = "/media/show/S01E01.disc", IsPlaceHolder = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Fact]
    public void HasChanged_ShortcutWithoutMediaInfo_ReturnsFalse()
    {
        // A .strm is only probed when remote content probing is enabled, so an empty one is expected.
        var item = new Episode { Path = "/media/show/S01E01.strm", IsShortcut = true };

        Assert.False(_probeProvider.HasChanged(item, Mock.Of<IDirectoryService>()));
    }

    [Theory]
    [InlineData(".strm")]
    [InlineData(".STRM")]
    public async Task FetchVideoInfo_AdditionalPartMissingShortcutFlag_ProbesTarget(string extension)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(directory.FullName, "Movie.part2" + extension);
            await File.WriteAllTextAsync(path, ShortcutTarget, cancellationToken);
            var item = new Video { Path = path, OwnerId = Guid.NewGuid() };
            var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
            {
                EnableRemoteContentProbe = true,
                MetadataRefreshMode = MetadataRefreshMode.ValidationOnly
            };
            _mediaEncoder.Setup(m => m.GetMediaInfo(It.IsAny<MediaInfoRequest>(), cancellationToken))
                .ReturnsAsync(new MediaBrowser.Model.MediaInfo.MediaInfo());

            await _probeProvider.FetchVideoInfo(item, options, cancellationToken);

            Assert.True(item.IsShortcut);
            Assert.Equal(ShortcutTarget, item.ShortcutPath);
            _mediaEncoder.Verify(
                m => m.GetMediaInfo(
                    It.Is<MediaInfoRequest>(r => r.MediaSource.Path == ShortcutTarget && r.MediaSource.Protocol == MediaProtocol.Http),
                    cancellationToken),
                Times.Once);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
