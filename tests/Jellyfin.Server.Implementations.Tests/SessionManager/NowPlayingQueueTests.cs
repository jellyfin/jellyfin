using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

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

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager()
    {
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(x => x.Configuration).Returns(new ServerConfiguration());

        return new(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            configManager.Object,
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
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
