using System;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using ImplementationSessionManager = Emby.Server.Implementations.Session.SessionManager;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class SessionManagerCompatibilityTests
{
    [Fact]
    public void OriginalReportingSignaturesRemainAvailableOnInterfaceAndImplementation()
    {
        var signatures = new[]
        {
            (Name: "OnPlaybackStart", Parameters: new[] { typeof(PlaybackStartInfo) }),
            (Name: "OnPlaybackProgress", Parameters: new[] { typeof(PlaybackProgressInfo) }),
            (Name: "OnPlaybackProgress", Parameters: new[] { typeof(PlaybackProgressInfo), typeof(bool) }),
            (Name: "OnPlaybackStopped", Parameters: new[] { typeof(PlaybackStopInfo) })
        };
        foreach (var type in new[] { typeof(ISessionManager), typeof(ImplementationSessionManager) })
        {
            foreach (var signature in signatures)
            {
                Assert.NotNull(type.GetMethod(signature.Name, signature.Parameters));
            }
        }

        var ownerOverload = typeof(ISessionManager).GetMethod("OnPlaybackStart", new[] { typeof(PlaybackStartInfo), typeof(string) });
        Assert.NotNull(ownerOverload);
        Assert.False(ownerOverload.IsAbstract);
    }

    [Fact]
    public async Task OriginalConstructorAndOwnerlessProgressRemainCallable()
    {
        var coordinator = new Emby.Server.Implementations.Session.VideoPlaybackCoordinator(TimeProvider.System, TimeSpan.FromMinutes(5));
        var user = new User("test", "default", "default");
        await using var services = CreateServices(coordinator, Mock.Of<ILiveStreamPlaybackManager>(), user).BuildServiceProvider();
        var constructor = typeof(ImplementationSessionManager).GetConstructors().Single(candidate => candidate.GetParameters().Length == 13);
        var arguments = constructor.GetParameters().Select(parameter => services.GetRequiredService(parameter.ParameterType)).ToArray();
        await using var manager = (ImplementationSessionManager)constructor.Invoke(arguments);
        var session = await manager.LogSessionActivity("Legacy", "1", "device", "device", "127.0.0.1", null);

        await manager.OnPlaybackProgress(new PlaybackProgressInfo { SessionId = session.Id, IsPaused = true }, true);

        Assert.True(session.PlayState.IsPaused);
    }

    [Fact]
    public async Task DependencyInjectionSelectsSharedCoordinatorAndSourceManagerConstructor()
    {
        var coordinator = new Emby.Server.Implementations.Session.VideoPlaybackCoordinator(TimeProvider.System, TimeSpan.FromMinutes(5));
        var ownership = new Mock<ILiveStreamPlaybackManager>();
        ownership.Setup(manager => manager.ClosePlaybackAsync("live", It.IsAny<VideoPlaybackContext>())).ReturnsAsync(true);
        var user = new User("test", "default", "default");
        await using var services = CreateServices(coordinator, ownership.Object, user)
            .AddSingleton<ISessionManager, ImplementationSessionManager>()
            .BuildServiceProvider();
        var manager = services.GetRequiredService<ISessionManager>();
        var session = await manager.LogSessionActivity("Modern", "1", "device", "device", "127.0.0.1", user);
        var playback = new VideoPlaybackContext(user.Id, "verified-owner", Guid.NewGuid(), "playback");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, "encoding", 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        await manager.OnPlaybackStopped(
            new PlaybackStopInfo { SessionId = session.Id, ItemId = playback.ItemId, PlaySessionId = "encoding", LiveStreamId = "live" },
            playback.SessionId,
            true);

        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.ResolvePlaySession(user.Id, playback.SessionId, playback.ItemId, "encoding", out _));
        ownership.Verify(source => source.ClosePlaybackAsync("live", playback), Times.Once);
    }

    private static IServiceCollection CreateServices(IVideoPlaybackCoordinator coordinator, ILiveStreamPlaybackManager liveStreams, User user)
    {
        var users = new Mock<IUserManager>();
        users.Setup(manager => manager.GetUserById(user.Id)).Returns(user);
        return new ServiceCollection()
            .AddSingleton<ILogger<ImplementationSessionManager>>(NullLogger<ImplementationSessionManager>.Instance)
            .AddSingleton(Mock.Of<IEventManager>())
            .AddSingleton(Mock.Of<IUserDataManager>())
            .AddSingleton(Mock.Of<IServerConfigurationManager>(config => config.Configuration == new ServerConfiguration()))
            .AddSingleton(Mock.Of<ILibraryManager>())
            .AddSingleton(users.Object)
            .AddSingleton(Mock.Of<IMusicManager>())
            .AddSingleton(Mock.Of<IDtoService>())
            .AddSingleton(Mock.Of<IImageProcessor>())
            .AddSingleton(Mock.Of<IServerApplicationHost>())
            .AddSingleton(Mock.Of<IDeviceManager>())
            .AddSingleton(Mock.Of<IMediaSourceManager>())
            .AddSingleton(Mock.Of<IHostApplicationLifetime>())
            .AddSingleton(coordinator)
            .AddSingleton(liveStreams);
    }
}
