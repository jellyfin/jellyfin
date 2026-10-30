using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Playlists;

public class PlaylistManagerTests
{
    [Fact]
    public void DetermineAdjustedIndexMoveToFirstPositionNoPriorInAllList()
    {
        var priorIndexAllChildren = 0;
        var newIndex = 0;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(0, adjustedIndex);
    }

    [Fact]
    public void DetermineAdjustedIndexPriorInMiddleOfAllList()
    {
        var priorIndexAllChildren = 2;
        var newIndex = 0;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(1, adjustedIndex);
    }

    [Fact]
    public void DetermineAdjustedIndexMoveMiddleOfPlaylist()
    {
        var priorIndexAllChildren = 2;
        var newIndex = 1;

        var adjustedIndex = PlaylistManager.DetermineAdjustedIndex(priorIndexAllChildren, newIndex);

        Assert.Equal(3, adjustedIndex);
    }

    [Fact]
    public void GetPlaylistsFolder_WhenInChildren_ReturnsFolder()
    {
        var libraryManagerMock = new Mock<ILibraryManager>();
        var rootFolder = new AggregateFolder();
        var playlistsFolder = new PlaylistsFolder();
        rootFolder.Children = [playlistsFolder];
        libraryManagerMock.Setup(x => x.RootFolder).Returns(rootFolder);

        var playlistManager = CreatePlaylistManager(libraryManagerMock);

        var result = playlistManager.GetPlaylistsFolder(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Same(playlistsFolder, result);
    }

    [Fact]
    public void GetPlaylistsFolder_WhenInVirtualChildren_ReturnsFolder()
    {
        var libraryManagerMock = new Mock<ILibraryManager>();
        var rootFolder = new AggregateFolder();
        rootFolder.Children = [];
        var playlistsFolder = new PlaylistsFolder();
        rootFolder.AddVirtualChild(playlistsFolder);
        libraryManagerMock.Setup(x => x.RootFolder).Returns(rootFolder);

        var playlistManager = CreatePlaylistManager(libraryManagerMock);

        var result = playlistManager.GetPlaylistsFolder(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Same(playlistsFolder, result);
    }

    [Fact]
    public void GetPlaylistsFolder_WhenInUserRootFolder_ReturnsFolder()
    {
        var libraryManagerMock = new Mock<ILibraryManager>();
        var rootFolder = new AggregateFolder();
        rootFolder.Children = [];
        libraryManagerMock.Setup(x => x.RootFolder).Returns(rootFolder);

        var userRootFolder = new AggregateFolder();
        var playlistsFolder = new PlaylistsFolder();
        userRootFolder.Children = [playlistsFolder];
        libraryManagerMock.Setup(x => x.GetUserRootFolder()).Returns(userRootFolder);

        var playlistManager = CreatePlaylistManager(libraryManagerMock);

        var result = playlistManager.GetPlaylistsFolder(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Same(playlistsFolder, result);
    }

    [Fact]
    public void GetPlaylistsFolder_WhenMissing_EnsuresAndCreatesFolder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JellyfinTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var libraryManagerMock = new Mock<ILibraryManager>();
            var rootFolder = new AggregateFolder();
            rootFolder.Children = [];
            libraryManagerMock.Setup(x => x.RootFolder).Returns(rootFolder);
            libraryManagerMock.Setup(x => x.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>())).Returns(Guid.NewGuid());

            var appPathsMock = new Mock<IApplicationPaths>();
            appPathsMock.Setup(x => x.DataPath).Returns(tempDir);

            var playlistManager = CreatePlaylistManager(libraryManagerMock, appPathsMock);

            var result = playlistManager.GetPlaylistsFolder(Guid.NewGuid());

            Assert.NotNull(result);
            Assert.IsType<PlaylistsFolder>(result);
            Assert.True(Directory.Exists(Path.Combine(tempDir, "playlists")));
            Assert.Contains(result, rootFolder.VirtualChildren);
            libraryManagerMock.Verify(x => x.RegisterItem(It.Is<BaseItem>(item => item == result)), Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task CreatePlaylist_WhenParentFolderInitiallyMissing_SucceedsWithoutException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JellyfinTests_" + Guid.NewGuid().ToString("N"));
        try
        {
            var libraryManagerMock = new Mock<ILibraryManager>();
            var rootFolder = new AggregateFolder();
            rootFolder.Children = [];
            libraryManagerMock.Setup(x => x.RootFolder).Returns(rootFolder);
            libraryManagerMock.Setup(x => x.GetNewItemId(It.IsAny<string>(), It.IsAny<Type>())).Returns(Guid.NewGuid());

            var appPathsMock = new Mock<IApplicationPaths>();
            appPathsMock.Setup(x => x.DataPath).Returns(tempDir);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.GetValidFilename(It.IsAny<string>())).Returns<string>(s => s);
            fileSystemMock.Setup(x => x.GetDirectoryPaths(It.IsAny<string>())).Returns([]);
            fileSystemMock.Setup(x => x.GetFilePaths(It.IsAny<string>())).Returns([]);
            fileSystemMock.Setup(x => x.GetFiles(It.IsAny<string>())).Returns([]);

            var userManagerMock = new Mock<IUserManager>();
            userManagerMock.Setup(x => x.GetUserById(It.IsAny<Guid>())).Returns(new User("TestUser", "DefaultAuth", "DefaultReset"));

            var playlistManager = CreatePlaylistManager(
                libraryManagerMock,
                appPathsMock,
                fileSystemMock,
                userManagerMock);

            var request = new PlaylistCreationRequest
            {
                Name = "AutoEnsuredPlaylist",
                UserId = Guid.NewGuid(),
                ItemIdList = []
            };

            var result = await playlistManager.CreatePlaylist(request);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.True(Directory.Exists(Path.Combine(tempDir, "playlists")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private static PlaylistManager CreatePlaylistManager(
        Mock<ILibraryManager> libraryManagerMock,
        Mock<IApplicationPaths>? appPathsMock = null,
        Mock<IFileSystem>? fileSystemMock = null,
        Mock<IUserManager>? userManagerMock = null,
        Mock<IProviderManager>? providerManagerMock = null)
    {
        fileSystemMock ??= new Mock<IFileSystem>();
        providerManagerMock ??= new Mock<IProviderManager>();
        userManagerMock ??= new Mock<IUserManager>();

        var mediaSourceManagerMock = new Mock<MediaBrowser.Controller.Library.IMediaSourceManager>();
        mediaSourceManagerMock.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaBrowser.Model.MediaInfo.MediaProtocol.File);

        BaseItem.LibraryManager = libraryManagerMock.Object;
        BaseItem.FileSystem = fileSystemMock.Object;
        BaseItem.ProviderManager = providerManagerMock.Object;
        BaseItem.MediaSourceManager = mediaSourceManagerMock.Object;
        BaseItem.Logger = new NullLogger<BaseItem>();

        providerManagerMock
            .Setup(x => x.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ItemUpdateType.None);

        libraryManagerMock
            .Setup(x => x.UpdateImagesAsync(It.IsAny<BaseItem>()))
            .Returns(Task.CompletedTask);

        var libraryMonitorMock = new Mock<ILibraryMonitor>();
        var logger = new NullLogger<PlaylistManager>();

        return new PlaylistManager(
            libraryManagerMock.Object,
            fileSystemMock.Object,
            libraryMonitorMock.Object,
            logger,
            userManagerMock.Object,
            providerManagerMock.Object,
            appPathsMock?.Object);
    }
}
