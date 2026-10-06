using System;
using System.Reflection;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

[Collection("LibraryManagerTests")]
public sealed class RewatchPlayStateTests : IDisposable
{
    private const long ResumeThresholdTicks = 100 * TimeSpan.TicksPerSecond;

    private static readonly DateTime _previousPlayDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly ILibraryManager? _previousLibraryManager;

    public RewatchPlayStateTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;

        // Resolves the (empty) alternate versions of the episode when the stop propagates its played state
        BaseItem.LibraryManager = Mock.Of<ILibraryManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
    }

    [Theory]
    [InlineData(3 * TimeSpan.TicksPerSecond, false)]
    [InlineData(600 * TimeSpan.TicksPerSecond, true)]
    public async Task RewatchOfPlayedEpisode_OnlyCountsPastResumeThreshold(long stopPositionTicks, bool expectDateUpdated)
    {
        var data = new UserItemData { Key = "episode", Played = true, LastPlayedDate = _previousPlayDate };
        await using var sessionManager = CreateSessionManager(data);
        var user = new User("test", "default", "default");
        var episode = new Episode { Id = Guid.NewGuid() };

        Invoke(sessionManager, "OnPlaybackStart", user, episode);
        Assert.Equal(_previousPlayDate, data.LastPlayedDate);

        Invoke(sessionManager, "OnPlaybackStopped", user, episode, stopPositionTicks, false);
        Assert.True(data.Played);
        Assert.Equal(expectDateUpdated, data.LastPlayedDate > _previousPlayDate);
    }

    [Fact]
    public async Task PlaybackStartOfUnplayedEpisode_UpdatesLastPlayedDate()
    {
        var data = new UserItemData { Key = "episode", LastPlayedDate = _previousPlayDate };
        await using var sessionManager = CreateSessionManager(data);

        Invoke(sessionManager, "OnPlaybackStart", new User("test", "default", "default"), new Episode { Id = Guid.NewGuid() });

        Assert.True(data.LastPlayedDate > _previousPlayDate);
    }

    private static Emby.Server.Implementations.Session.SessionManager CreateSessionManager(UserItemData data)
    {
        var userDataManager = new Mock<IUserDataManager>();
        userDataManager
            .Setup(m => m.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
            .Returns(data);
        userDataManager
            .Setup(m => m.UpdatePlayState(It.IsAny<BaseItem>(), It.IsAny<UserItemData>(), It.IsAny<long?>()))
            .Callback<BaseItem, UserItemData, long?>((_, d, position) => d.PlaybackPositionTicks = position >= ResumeThresholdTicks ? position.Value : 0)
            .Returns(false);

        return new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            userDataManager.Object,
            Mock.Of<IServerConfigurationManager>(),
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

    private static void Invoke(Emby.Server.Implementations.Session.SessionManager sessionManager, string name, params object?[] args)
    {
        var parameterTypes = Array.ConvertAll(args, a => a!.GetType());
        parameterTypes[1] = typeof(BaseItem);
        if (args.Length > 2)
        {
            parameterTypes[2] = typeof(long?);
        }

        typeof(Emby.Server.Implementations.Session.SessionManager)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, parameterTypes)!
            .Invoke(sessionManager, args);
    }
}
