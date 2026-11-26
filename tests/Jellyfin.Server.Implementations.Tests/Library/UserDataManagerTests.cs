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

    [Fact]
    public void SaveUserData_RowUnderRetiredKey_IsKeptInAgreement()
    {
        var item = CreateAudioBook();

        using (var ctx = CreateDbContext())
        {
            ctx.Users.Add(_user);
            ctx.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = typeof(AudioBook).FullName! });
            ctx.UserData.Add(CreateUserDataRow(item, "Author-Old Album-0001Old File Name", 111));
            ctx.SaveChanges();
        }

        _userDataManager.SaveUserData(
            _user,
            item,
            new UserItemData { Key = item.GetUserDataKeys()[0], Played = true },
            UserDataSaveReason.UpdateUserRating,
            CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(item.Id)).ToList();

            // The retired-key row is what a re-added item reattaches by, so it survives, but it must
            // not keep a playback position that holds the item in Continue Watching.
            Assert.Equal(
                item.GetUserDataKeys().Append("Author-Old Album-0001Old File Name").OrderBy(e => e, StringComparer.Ordinal),
                rows.Select(e => e.CustomDataKey).OrderBy(e => e, StringComparer.Ordinal));
            Assert.All(rows, row =>
            {
                Assert.True(row.Played);
                Assert.Equal(0, row.PlaybackPositionTicks);
            });
        }
    }

    [Fact]
    public void SaveUserData_ManyItems_WritesEveryKeyInOneGoAndRaisesPerItem()
    {
        var items = new[] { CreateAudioBook(), CreateAudioBook(), CreateAudioBook() };
        var retiredKey = "Author-Old Album-0001Old File Name";

        using (var ctx = CreateDbContext())
        {
            ctx.Users.Add(_user);
            foreach (var item in items)
            {
                ctx.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = typeof(AudioBook).FullName! });
            }

            // one item already has a row under its current key, another only under a retired key
            ctx.UserData.AddRange(
                CreateUserDataRow(items[0], items[0].GetUserDataKeys()[0], 111),
                CreateUserDataRow(items[1], retiredKey, 222));
            ctx.SaveChanges();
        }

        var raised = new List<BaseItem>();
        _userDataManager.UserDataSaved += (_, e) => raised.Add(e.Item);

        _userDataManager.SaveUserData(
            _user,
            items.Select(item => ((BaseItem)item, new UserItemData { Key = item.GetUserDataKeys()[0], Played = true, PlayCount = 1 })).ToList(),
            UserDataSaveReason.TogglePlayed,
            CancellationToken.None);

        Assert.Equal(items.Select(i => i.Id), raised.Select(i => i.Id));

        using (var ctx = CreateDbContext())
        {
            foreach (var item in items)
            {
                var rows = ctx.UserData.Where(e => e.ItemId.Equals(item.Id)).ToList();
                var expectedKeys = item.GetUserDataKeys();
                if (item == items[1])
                {
                    // the retired-key row survives and is kept in agreement
                    expectedKeys = expectedKeys.Append(retiredKey).ToList();
                }

                Assert.Equal(
                    expectedKeys.OrderBy(e => e, StringComparer.Ordinal),
                    rows.Select(e => e.CustomDataKey).OrderBy(e => e, StringComparer.Ordinal));
                Assert.All(rows, row =>
                {
                    Assert.True(row.Played);
                    Assert.Equal(1, row.PlayCount);
                    Assert.Equal(0, row.PlaybackPositionTicks);
                });
            }
        }

        // the instances and the cache see the saved state without another query
        Assert.All(items, item => Assert.True(_userDataManager.GetUserData(_user, item)!.Played));
        Assert.All(_userDataManager.GetUserDataBatch(items, _user).Values, data => Assert.True(data.Played));
    }

    [Fact]
    public void SaveUserData_NoItems_DoesNothing()
    {
        var raised = 0;
        _userDataManager.UserDataSaved += (_, _) => raised++;

        _userDataManager.SaveUserData(_user, [], UserDataSaveReason.TogglePlayed, CancellationToken.None);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void GetUserData_NullUser_ThrowsArgumentNullException()
    {
        var item = CreateAudioBook();
        Assert.Throws<ArgumentNullException>(() => _userDataManager.GetUserData(null!, item));
    }
}
