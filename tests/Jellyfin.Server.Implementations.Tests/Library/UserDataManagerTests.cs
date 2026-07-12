using System;
using System.Collections.Generic;
using System.Threading;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
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

        var config = new Mock<IServerConfigurationManager>();
        config.SetupGet(c => c.Configuration).Returns(new ServerConfiguration
        {
            MinResumePct = 5,
            MaxResumePct = 90,
            MinResumeDurationSeconds = 300,
            MinAudiobookResume = 5,
            MaxAudiobookResume = 5
        });

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

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

    private void SeedCommittedPosition(Guid itemId, Guid userId, long positionTicks)
    {
        using var dbContext = CreateDbContext();
        dbContext.BaseItems.Add(new BaseItemEntity { Id = itemId, Type = typeof(AudioBook).FullName! });
        dbContext.Users.Add(new User("seed-user", "default", "default") { Id = userId });
        dbContext.UserData.Add(new UserData
        {
            ItemId = itemId,
            UserId = userId,
            CustomDataKey = "test-key",
            PlaybackPositionTicks = positionTicks,
            Item = null,
            User = null
        });
        dbContext.SaveChanges();
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

    private static AudioBook CreateAudioBook(TimeSpan runtime)
    {
        return new AudioBook
        {
            RunTimeTicks = runtime.Ticks
        };
    }

    private static Movie CreateMovie(TimeSpan runtime)
    {
        return new Movie
        {
            RunTimeTicks = runtime.Ticks
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

    private static UserItemData CreateUserData(long positionTicks)
    {
        return new UserItemData
        {
            Key = "test-key",
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
    public void UpdatePlayState_AudioBookNearStartReportAfterLongResume_ResetsToZero()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, TimeSpan.FromSeconds(20).Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
        Assert.False(data.Played);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNearStartReportWithNoExistingResume_StaysZero()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var data = CreateUserData(0);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, TimeSpan.FromSeconds(20).Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookLiteralZeroReportAfterLongResume_HonorsExplicitRestart()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, 0);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookStopAtZeroAfterResume_PreservesResume()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(18));
        item.Id = Guid.NewGuid();
        var existingResume = TimeSpan.FromHours(4) + TimeSpan.FromMinutes(18);
        var data = CreateUserData(existingResume.Ticks);
        var user = new User("test", "default", "default");
        SeedCommittedPosition(item.Id, user.Id, existingResume.Ticks);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, 0, user, wasStopped: true);

        Assert.Equal(existingResume.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
        Assert.False(data.Played);
    }

    [Fact]
    public void UpdatePlayState_AudioBookProgressAtZeroAfterResume_HonorsRestart()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(18));
        var existingResume = TimeSpan.FromHours(4) + TimeSpan.FromMinutes(18);
        var data = CreateUserData(existingResume.Ticks);
        var user = new User("test", "default", "default");

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, 0, user, wasStopped: false);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNormalForwardProgress_UpdatesPosition()
    {
        var item = CreateAudioBook(TimeSpan.FromHours(16));
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromHours(6);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(reported.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_AudioBookNearEndReport_MarksCompletedAndClearsResume()
    {
        var runtime = TimeSpan.FromHours(16);
        var item = CreateAudioBook(runtime);
        var existingResume = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(49);
        var data = CreateUserData(existingResume.Ticks);
        var reported = runtime - TimeSpan.FromMinutes(2);

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.True(playedToCompletion);
        Assert.True(data.Played);
    }

    [Fact]
    public void UpdatePlayState_MovieBelowMinResumePctAfterHalfwayResume_ResetsToZero()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.01));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_MovieGenuineBackwardSeek_PreservesReportedPosition()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.40));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(reported.Ticks, data.PlaybackPositionTicks);
        Assert.False(playedToCompletion);
    }

    [Fact]
    public void UpdatePlayState_MovieAboveMaxResumePct_MarksCompletedAndClearsResume()
    {
        var runtime = TimeSpan.FromHours(2);
        var item = CreateMovie(runtime);
        var existingResume = TimeSpan.FromTicks(runtime.Ticks / 2);
        var data = CreateUserData(existingResume.Ticks);
        var reported = TimeSpan.FromTicks((long)(runtime.Ticks * 0.96));

        var playedToCompletion = _userDataManager.UpdatePlayState(item, data, reported.Ticks);

        Assert.Equal(0, data.PlaybackPositionTicks);
        Assert.True(playedToCompletion);
        Assert.True(data.Played);
    }
}
