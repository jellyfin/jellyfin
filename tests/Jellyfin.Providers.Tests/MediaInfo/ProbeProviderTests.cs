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
    private readonly ProbeProvider _probeProvider;

    public ProbeProviderTests()
    {
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);

        // prep BaseItem and Video for calls made that expect managers
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();

        _probeProvider = new ProbeProvider(
            mediaSourceManager.Object,
            Mock.Of<IMediaEncoder>(),
            Mock.Of<IBlurayExaminer>(),
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IChapterManager>(),
            Mock.Of<IServerConfigurationManager>(),
            Mock.Of<ISubtitleManager>(),
            Mock.Of<ILibraryManager>(),
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
}
