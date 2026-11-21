using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

/// <summary>
/// A session keeps the item instance it started playing, while API calls such as marking a favorite
/// load their own instance. Progress and stop must not write the session's stale copy back over
/// those changes (https://github.com/jellyfin/jellyfin/issues/14981).
/// </summary>
public sealed class UserDataDuringPlaybackTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly UserDataManager _userDataManager;
    private readonly Emby.Server.Implementations.Session.SessionManager _sessionManager;
    private readonly User _user;
    private readonly Guid _itemId = Guid.NewGuid();

    public UserDataDuringPlaybackTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        _user = new User("user", "auth-provider", "reset-provider")
        {
            Id = Guid.NewGuid()
        };

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            ctx.Users.Add(_user);
            ctx.BaseItems.Add(new BaseItemEntity { Id = _itemId, Type = typeof(Audio).FullName! });
            ctx.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration());

        _userDataManager = new UserDataManager(config.Object, factory.Object);
        _sessionManager = new Emby.Server.Implementations.Session.SessionManager(
            NullLogger<Emby.Server.Implementations.Session.SessionManager>.Instance,
            Mock.Of<IEventManager>(),
            _userDataManager,
            config.Object,
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

    public async ValueTask DisposeAsync()
    {
        await _sessionManager.DisposeAsync();
        _connection.Dispose();
    }

    [Fact]
    public void FavoriteMarkedDuringPlayback_SurvivesStop()
    {
        var sessionCopy = CreateSong();
        Invoke("OnPlaybackStart", [typeof(User), typeof(BaseItem)], _user, sessionCopy);

        MarkFavoriteThroughAnotherInstance();

        Invoke(
            "OnPlaybackStopped",
            [typeof(User), typeof(BaseItem), typeof(long?), typeof(bool)],
            _user,
            sessionCopy,
            30 * TimeSpan.TicksPerSecond,
            false);

        var saved = SavedUserData();
        Assert.True(saved.IsFavorite);
        Assert.Equal(1, saved.PlayCount);
    }

    [Fact]
    public void FavoriteMarkedDuringPlayback_SurvivesProgress()
    {
        var sessionCopy = CreateSong();
        Invoke("OnPlaybackStart", [typeof(User), typeof(BaseItem)], _user, sessionCopy);

        MarkFavoriteThroughAnotherInstance();

        Invoke(
            "OnPlaybackProgress",
            [typeof(User), typeof(BaseItem), typeof(PlaybackProgressInfo)],
            _user,
            sessionCopy,
            new PlaybackProgressInfo { ItemId = _itemId, PositionTicks = 10 * TimeSpan.TicksPerSecond });

        Assert.True(SavedUserData().IsFavorite);
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    // Songs aren't kept in the library manager's item cache, so every lookup builds a new instance.
    private Audio CreateSong()
    {
        return new Audio
        {
            Id = _itemId,
            Name = "Song",
            Album = "Album",
            AlbumArtists = ["Artist"],
            IndexNumber = 1,
            RunTimeTicks = 180 * TimeSpan.TicksPerSecond
        };
    }

    // What the favorite endpoint does: load the item (with its user data) and save the change.
    private void MarkFavoriteThroughAnotherInstance()
    {
        var item = CreateSong();
        using (var ctx = CreateDbContext())
        {
            item.UserData = ctx.UserData.Where(e => e.ItemId.Equals(_itemId)).AsNoTracking().ToArray();
        }

        var data = _userDataManager.GetUserData(_user, item)!;
        data.IsFavorite = true;
        _userDataManager.SaveUserData(_user, item, data, UserDataSaveReason.UpdateUserRating, CancellationToken.None);
    }

    private UserData SavedUserData()
    {
        using var ctx = CreateDbContext();
        var key = CreateSong().GetUserDataKeys()[0];
        return ctx.UserData.Single(e => e.ItemId.Equals(_itemId) && e.UserId.Equals(_user.Id) && e.CustomDataKey == key);
    }

    private void Invoke(string name, Type[] parameterTypes, params object?[] args)
    {
        typeof(Emby.Server.Implementations.Session.SessionManager)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, parameterTypes)!
            .Invoke(_sessionManager, args);
    }
}
