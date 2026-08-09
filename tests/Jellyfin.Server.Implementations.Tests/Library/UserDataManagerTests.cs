using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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

    /// <summary>
    /// Creates an audio book. Passing an existing <paramref name="id"/> produces a second
    /// <see cref="BaseItem"/> instance for the same library item, which is what the playback
    /// session and the rest of the server routinely hold at the same time.
    /// </summary>
    private AudioBook CreateAudioBook(Guid? id = null)
    {
        // GetUserDataKeys(): ["Author-Series-0001Book Title", "<item id N>"]
        return new AudioBook
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Book Title",
            Album = "Series",
            AlbumArtists = new[] { "Author" },
            IndexNumber = 1
        };
    }

    private UserData CreateUserDataRow(AudioBook item, string key, long positionTicks, bool isFavorite = false)
    {
        return new UserData
        {
            ItemId = item.Id,
            Item = null,
            UserId = _user.Id,
            User = null,
            CustomDataKey = key,
            PlaybackPositionTicks = positionTicks,
            IsFavorite = isFavorite
        };
    }

    /// <summary>
    /// Inserts the foreign key rows <see cref="UserDataManager.SaveUserData(User, BaseItem, UserItemData, UserDataSaveReason, CancellationToken)"/>
    /// depends on, plus any user data rows the test wants present without going through the manager.
    /// </summary>
    private void SeedDatabase(AudioBook item, params UserData[] rows)
    {
        using var ctx = CreateDbContext();
        ctx.Users.Add(_user);

        // rows belonging to another user need that user to exist for the foreign key to hold
        foreach (var userId in rows.Select(e => e.UserId).Distinct().Where(id => !id.Equals(_user.Id)))
        {
            ctx.Users.Add(new User("user-" + userId.ToString("N"), "auth-provider", "reset-provider") { Id = userId });
        }

        ctx.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = typeof(AudioBook).FullName! });
        ctx.UserData.AddRange(rows);
        ctx.SaveChanges();
    }

    private UserData ReadRow(AudioBook item, string key)
    {
        using var ctx = CreateDbContext();
        return ctx.UserData.AsNoTracking().Single(e => e.ItemId.Equals(item.Id) && e.CustomDataKey == key);
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

    [Fact]
    public void GetUserData_NullUser_ThrowsArgumentNullException()
    {
        var item = CreateAudioBook();
        Assert.Throws<ArgumentNullException>(() => _userDataManager.GetUserData(null!, item));
    }

    [Fact]
    public void SaveUserData_StaleInstanceProgressSave_DoesNotClobberFavorite()
    {
        var favoriteView = CreateAudioBook();
        SeedDatabase(favoriteView);

        // the instance the favorite request loaded
        var favoriteData = _userDataManager.GetUserData(_user, favoriteView);
        Assert.NotNull(favoriteData);
        favoriteData.IsFavorite = true;
        _userDataManager.SaveUserData(_user, favoriteView, favoriteData, UserDataSaveReason.UpdateUserRating, CancellationToken.None);

        // the instance the playback session pinned before the favorite was set, so its rows
        // still say the item is not a favorite
        var sessionView = CreateAudioBook(favoriteView.Id);
        Assert.Empty(sessionView.UserData);

        var progressData = _userDataManager.GetUserData(_user, sessionView);
        Assert.NotNull(progressData);
        progressData.PlaybackPositionTicks = 123;
        _userDataManager.SaveUserData(_user, sessionView, progressData, UserDataSaveReason.PlaybackProgress, CancellationToken.None);

        var row = ReadRow(favoriteView, favoriteView.GetUserDataKeys()[0]);
        Assert.True(row.IsFavorite);
        Assert.Equal(123, row.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_NullNavigationRows_FallsBackToDatabase()
    {
        var item = CreateAudioBook();
        SeedDatabase(item, CreateUserDataRow(item, item.GetUserDataKeys()[0], 0, isFavorite: true));

        // an item materialized by a query that did not include its user data
        var detached = CreateAudioBook(item.Id);
        detached.UserData = null!;

        var userData = _userDataManager.GetUserData(_user, detached);

        Assert.NotNull(userData);
        Assert.True(userData.IsFavorite);
    }

    [Fact]
    public void SaveUserData_DtoWithOnlyPosition_PreservesFavoriteOnNullNavigationRows()
    {
        var item = CreateAudioBook();
        SeedDatabase(item, CreateUserDataRow(item, item.GetUserDataKeys()[0], 0, isFavorite: true));

        // seeded directly rather than through the manager so this exercises the database
        // fallback instead of the cache
        var detached = CreateAudioBook(item.Id);
        detached.UserData = null!;

        _userDataManager.SaveUserData(
            _user,
            detached,
            new UpdateUserItemDataDto { PlaybackPositionTicks = 999 },
            UserDataSaveReason.UpdateUserData);

        var row = ReadRow(item, item.GetUserDataKeys()[0]);
        Assert.True(row.IsFavorite);
        Assert.Equal(999, row.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_NullItem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _userDataManager.GetUserData(_user, null!));
    }

    [Fact]
    public void GetUserData_NullNavigationRows_PrefersCurrentKeyRow()
    {
        var item = CreateAudioBook();

        // the id-key row is inserted first so selection by row order would return it
        SeedDatabase(
            item,
            CreateUserDataRow(item, item.GetUserDataKeys()[1], 111),
            CreateUserDataRow(item, item.GetUserDataKeys()[0], 222));

        var detached = CreateAudioBook(item.Id);
        detached.UserData = null!;

        var userData = _userDataManager.GetUserData(_user, detached);

        Assert.NotNull(userData);
        Assert.Equal(222, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_NullNavigationRows_IgnoresOtherUsersRows()
    {
        var item = CreateAudioBook();
        var otherUserRow = CreateUserDataRow(item, item.GetUserDataKeys()[0], 999, isFavorite: true);
        otherUserRow.UserId = Guid.NewGuid();

        SeedDatabase(item, otherUserRow);

        var detached = CreateAudioBook(item.Id);
        detached.UserData = null!;

        var userData = _userDataManager.GetUserData(_user, detached);

        Assert.NotNull(userData);
        Assert.False(userData.IsFavorite);
        Assert.Equal(0, userData.PlaybackPositionTicks);
    }

    [Fact]
    public void GetUserData_EmptyNavigationRows_DoesNotConsultDatabase()
    {
        // An empty collection is a hydrated "this item has no rows" answer, so it is taken at face
        // value. Querying on every empty collection instead would put a database round trip inside
        // the user data sort comparers, which call this once per comparison.
        var item = CreateAudioBook();
        SeedDatabase(item, CreateUserDataRow(item, item.GetUserDataKeys()[0], 222, isFavorite: true));

        var detached = CreateAudioBook(item.Id);
        Assert.Empty(detached.UserData);

        var userData = _userDataManager.GetUserData(_user, detached);

        Assert.NotNull(userData);
        Assert.False(userData.IsFavorite);
        Assert.Equal(item.GetUserDataKeys()[0], userData.Key);
    }

    [Fact]
    public void GetUserDataBatch_AfterSaveThroughAnotherInstance_ReturnsSavedData()
    {
        var item = CreateAudioBook();
        SeedDatabase(item);

        var userData = _userDataManager.GetUserData(_user, item);
        Assert.NotNull(userData);
        userData.IsFavorite = true;
        _userDataManager.SaveUserData(_user, item, userData, UserDataSaveReason.UpdateUserRating, CancellationToken.None);

        var sessionView = CreateAudioBook(item.Id);

        var result = _userDataManager.GetUserDataBatch([sessionView], _user);

        Assert.True(result[item.Id].IsFavorite);
    }

    [Fact]
    public void SaveUserData_MultiKeyItem_KeepsPrimaryKeyOnSubsequentReads()
    {
        var item = CreateAudioBook();
        SeedDatabase(item);
        Assert.Equal(2, item.GetUserDataKeys().Count);

        var userData = _userDataManager.GetUserData(_user, item);
        Assert.NotNull(userData);
        userData.PlayCount = 3;
        _userDataManager.SaveUserData(_user, item, userData, UserDataSaveReason.TogglePlayed, CancellationToken.None);

        var reread = _userDataManager.GetUserData(_user, item);

        Assert.NotNull(reread);
        Assert.Equal(item.GetUserDataKeys()[0], reread.Key);
    }
}
