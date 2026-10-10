using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using SyncPlayGroup = Emby.Server.Implementations.SyncPlay.Group;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class NowPlayingQueueTests
{
    [Fact]
    public async Task OnPlaybackStart_StoresNowPlayingQueue()
    {
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);
        var queue = CreateQueue(3);

        await sessionManager.OnPlaybackStart(new PlaybackStartInfo
        {
            SessionId = session.Id,
            NowPlayingQueue = queue
        });

        Assert.Equal(queue, session.NowPlayingQueue);
    }

    [Fact]
    public async Task OnPlaybackProgress_WithoutQueue_KeepsNowPlayingQueue()
    {
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);
        var queue = CreateQueue(3);

        await sessionManager.OnPlaybackProgress(new PlaybackProgressInfo
        {
            SessionId = session.Id,
            NowPlayingQueue = queue
        });
        await sessionManager.OnPlaybackProgress(new PlaybackProgressInfo
        {
            SessionId = session.Id
        });

        Assert.Equal(queue, session.NowPlayingQueue);
    }

    [Fact]
    public async Task CreateGroup_AfterPlaybackStart_StartsFromReportedQueue()
    {
        var queue = CreateQueue(3);
        var playingItem = new Mock<BaseItem>().Object;
        playingItem.Id = queue[1].Id;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetItemById(playingItem.Id)).Returns(playingItem);
        var dtoService = new Mock<IDtoService>();
        dtoService
            .Setup(x => x.GetBaseItemDto(It.IsAny<BaseItem>(), It.IsAny<DtoOptions>(), It.IsAny<User?>(), It.IsAny<BaseItem?>()))
            .Returns(new BaseItemDto());

        await using var sessionManager = CreateSessionManager(libraryManager.Object, dtoService.Object);
        var session = await CreateSession(sessionManager);

        await sessionManager.OnPlaybackStart(new PlaybackStartInfo
        {
            SessionId = session.Id,
            ItemId = playingItem.Id,
            NowPlayingQueue = queue
        });

        var group = new SyncPlayGroup(
            NullLoggerFactory.Instance,
            Mock.Of<IUserManager>(),
            Mock.Of<ISessionManager>(),
            Mock.Of<ILibraryManager>());
        group.CreateGroup(session, new NewGroupRequest("group"), CancellationToken.None);

        Assert.Equal(queue.Select(item => item.Id), group.PlayQueue.GetPlaylist().Select(item => item.ItemId));
        Assert.Equal(1, group.PlayQueue.PlayingItemIndex);
    }

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(
        ILibraryManager? libraryManager = null,
        IDtoService? dtoService = null)
    {
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(x => x.Configuration).Returns(new ServerConfiguration());

        return new(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            configManager.Object,
            libraryManager ?? Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            dtoService ?? Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            Mock.Of<IMediaSourceManager>(),
            Mock.Of<IHostApplicationLifetime>());
    }

    private static Task<SessionInfo> CreateSession(Emby.Server.Implementations.Session.SessionManager sessionManager)
        => sessionManager.LogSessionActivity(
            "Test Client",
            "1.0.0",
            "test-device",
            "Test Device",
            "127.0.0.1",
            null);

    private static QueueItem[] CreateQueue(int count)
        => Enumerable.Range(0, count)
            .Select(i => new QueueItem
            {
                Id = Guid.NewGuid(),
                PlaylistItemId = "playlistItem" + i
            })
            .ToArray();
}
