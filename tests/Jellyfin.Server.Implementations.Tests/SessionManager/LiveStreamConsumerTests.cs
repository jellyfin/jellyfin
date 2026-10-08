using System.Collections.Generic;
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

public class LiveStreamConsumerTests
{
    private const string LiveStreamId = "live-stream";

    private readonly HashSet<string> _openedPlaySessions = new();
    private readonly Mock<IMediaSourceManager> _mediaSourceManager = new();

    public LiveStreamConsumerTests()
    {
        _mediaSourceManager
            .Setup(m => m.IsLiveStreamOpenedFor(LiveStreamId, It.IsAny<string>()))
            .Returns((string _, string playSessionId) => _openedPlaySessions.Contains(playSessionId));
        _mediaSourceManager
            .Setup(m => m.CloseLiveStream(LiveStreamId, It.IsAny<string>()))
            .ReturnsAsync((string _, string playSessionId) => _openedPlaySessions.Remove(playSessionId));
    }

    [Fact]
    public async Task OnPlaybackStart_ReplacingPlaySessionOpenedOwnConsumer_ClosesReplacedConsumer()
    {
        _openedPlaySessions.UnionWith(["play-1", "play-2"]);
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);

        await sessionManager.OnPlaybackStart(Start(session, "play-1"));
        await sessionManager.OnPlaybackStart(Start(session, "play-2"));
        await sessionManager.OnPlaybackStopped(Stop(session, "play-2"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(LiveStreamId, "play-1"), Times.Once);
        _mediaSourceManager.Verify(m => m.CloseLiveStream(LiveStreamId), Times.Once);
    }

    [Fact]
    public async Task OnPlaybackProgress_ReplacingPlaySessionReusedStream_KeepsConsumer()
    {
        _openedPlaySessions.Add("play-1");
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);

        await sessionManager.OnPlaybackStart(Start(session, "play-1"));
        await sessionManager.OnPlaybackProgress(Start(session, "play-2"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mediaSourceManager.Verify(m => m.CloseLiveStream(It.IsAny<string>()), Times.Never);

        await sessionManager.OnPlaybackStopped(Stop(session, "play-2"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(LiveStreamId), Times.Once);
    }

    [Fact]
    public async Task OnPlaybackProgress_SamePlaySession_KeepsConsumer()
    {
        _openedPlaySessions.Add("play-1");
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);

        await sessionManager.OnPlaybackStart(Start(session, "play-1"));
        await sessionManager.OnPlaybackProgress(Start(session, "play-1"));
        await sessionManager.OnPlaybackProgress(Start(session, "play-1"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mediaSourceManager.Verify(m => m.CloseLiveStream(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPlaybackStopped_ReplacedPlaySession_KeepsReplacingConsumer()
    {
        _openedPlaySessions.UnionWith(["play-1", "play-2"]);
        await using var sessionManager = CreateSessionManager();
        var session = await CreateSession(sessionManager);

        await sessionManager.OnPlaybackStart(Start(session, "play-1"));
        await sessionManager.OnPlaybackStart(Start(session, "play-2"));
        await sessionManager.OnPlaybackStopped(Stop(session, "play-1"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(LiveStreamId, "play-1"), Times.Once);
        _mediaSourceManager.Verify(m => m.CloseLiveStream(It.IsAny<string>()), Times.Never);

        await sessionManager.OnPlaybackStopped(Stop(session, "play-2"));

        _mediaSourceManager.Verify(m => m.CloseLiveStream(LiveStreamId), Times.Once);
    }

    private static PlaybackStartInfo Start(SessionInfo session, string playSessionId)
        => new() { SessionId = session.Id, LiveStreamId = LiveStreamId, PlaySessionId = playSessionId };

    private static PlaybackStopInfo Stop(SessionInfo session, string playSessionId)
        => new() { SessionId = session.Id, LiveStreamId = LiveStreamId, PlaySessionId = playSessionId };

    private static Task<SessionInfo> CreateSession(Emby.Server.Implementations.Session.SessionManager sessionManager)
        => sessionManager.LogSessionActivity("Test Client", "1.0.0", "test-device", "Test Device", "127.0.0.1", null);

    private Emby.Server.Implementations.Session.SessionManager CreateSessionManager()
        => new(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            Mock.Of<IUserDataManager>(),
            Mock.Of<IServerConfigurationManager>(c => c.Configuration == new ServerConfiguration()),
            Mock.Of<ILibraryManager>(),
            Mock.Of<IUserManager>(),
            Mock.Of<IMusicManager>(),
            Mock.Of<IDtoService>(),
            Mock.Of<IImageProcessor>(),
            Mock.Of<IServerApplicationHost>(),
            Mock.Of<IDeviceManager>(),
            _mediaSourceManager.Object,
            Mock.Of<IHostApplicationLifetime>());
}
