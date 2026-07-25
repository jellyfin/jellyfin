using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AudioBook = MediaBrowser.Controller.Entities.AudioBook;

namespace Jellyfin.Server.Implementations.Tests.Library;

public sealed class UserDataManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly UserDataManager _userDataManager;
    private readonly User _user;

    public UserDataManagerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration());

        _userDataManager = new UserDataManager(config.Object, factory.Object);
        _user = new User("user", "auth-provider", "reset-provider")
        {
            Id = Guid.NewGuid()
        };
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private AudioBook CreateAudioBook()
    {
        // GetUserDataKeys(): ["Author-Series-0001Book Title", "<item id N>"]
        return new AudioBook
        {
            Id = Guid.NewGuid(),
            Name = "Book Title",
            Album = "Series",
            AlbumArtists = new[] { "Author" },
            IndexNumber = 1
        };
    }

    private UserData CreateUserDataRow(AudioBook item, string key, long positionTicks)
    {
        return new UserData
        {
            ItemId = item.Id,
            Item = null,
            UserId = _user.Id,
            User = null,
            CustomDataKey = key,
            PlaybackPositionTicks = positionTicks
        };
    }

    [Fact]
    public void GetUserData_RowsUnderCurrentAndRetiredKeys_PrefersCurrentKeyRow()
    {
        var item = CreateAudioBook();
        var currentKey = item.GetUserDataKeys()[0];

        // the retired-key row comes first to ensure selection is by key, not row order
        item.UserData = new List<UserData>
        {
            CreateUserDataRow(item, "Author-Old Album-0001Old File Name", 111),
            CreateUserDataRow(item, currentKey, 222)
        };

        var userData = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(userData);
        Assert.Equal(currentKey, userData.Key);
        Assert.Equal(222, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_NoPrimaryKeyRow_UsesNextCurrentKeyRow()
    {
        var item = CreateAudioBook();
        var idKey = item.GetUserDataKeys()[1];

        item.UserData = new List<UserData>
        {
            CreateUserDataRow(item, "Author-Old Album-0001Old File Name", 111),
            CreateUserDataRow(item, idKey, 333)
        };

        var userData = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(userData);
        Assert.Equal(idKey, userData.Key);
        Assert.Equal(333, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_OnlyRetiredKeyRows_ReturnsRetiredKeyRow()
    {
        var item = CreateAudioBook();

        item.UserData = new List<UserData>
        {
            CreateUserDataRow(item, "Author-Old Album-0001Old File Name", 111)
        };

        var userData = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(userData);
        Assert.Equal(111, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_NoRows_ReturnsDefaultWithPrimaryKey()
    {
        var item = CreateAudioBook();
        item.UserData = new List<UserData>();

        var userData = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(userData);
        Assert.Equal(item.GetUserDataKeys()[0], userData.Key);
        Assert.Equal(0, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_RowsForOtherUsers_AreIgnored()
    {
        var item = CreateAudioBook();
        var currentKey = item.GetUserDataKeys()[0];

        var otherUserRow = CreateUserDataRow(item, currentKey, 999);
        otherUserRow.UserId = Guid.NewGuid();

        item.UserData = new List<UserData>
        {
            otherUserRow,
            CreateUserDataRow(item, currentKey, 222)
        };

        var userData = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(userData);
        Assert.Equal(222, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserDataBatch_DatabaseFallback_ResolvesRowsByKeyOrder()
    {
        // no preloaded navigation data, so the batch takes the database fallback
        var fossilItem = CreateAudioBook();
        var retiredItem = CreateAudioBook();

        using (var ctx = CreateDbContext())
        {
            ctx.Users.Add(_user);
            ctx.BaseItems.Add(new BaseItemEntity { Id = fossilItem.Id, Type = typeof(AudioBook).FullName! });
            ctx.BaseItems.Add(new BaseItemEntity { Id = retiredItem.Id, Type = typeof(AudioBook).FullName! });

            // the stale id-key row is inserted first so selection by row order would return it
            ctx.UserData.AddRange(
                CreateUserDataRow(fossilItem, fossilItem.GetUserDataKeys()[1], 111),
                CreateUserDataRow(fossilItem, fossilItem.GetUserDataKeys()[0], 222),
                CreateUserDataRow(retiredItem, "Author-Old Album-0001Old File Name", 333));
            ctx.SaveChanges();
        }

        var result = _userDataManager.GetUserDataBatch([fossilItem, retiredItem], _user);

        Assert.Equal(222, result[fossilItem.Id].PlaybackPositionTicks);
        Assert.Equal(333, result[retiredItem.Id].PlaybackPositionTicks);
    }
}
