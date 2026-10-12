using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A value that is matched against stored rows is cleaned of NUL characters first, so a value that differs
/// from a stored one only by a NUL finds the row instead of being added again.
/// </summary>
public sealed class ItemPersistenceNulMatchingTests : JellyfinDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ItemPersistenceNulMatchingTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Theory]
    [InlineData("Rock\0")]
    [InlineData("Ro\0ck")]
    [InlineData("\0Rock")]
    public void SaveItems_GenreDifferingOnlyByNulFromStoredValue_MapsToTheStoredValue(string genre)
    {
        var first = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), genres: ["Rock"]);
        var second = CreateBook(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), genres: [genre]);
        _service.SaveItems([first], CancellationToken.None);

        _service.SaveItems([second], CancellationToken.None);

        using var ctx = CreateDbContext();
        var value = Assert.Single(ctx.ItemValues);
        Assert.Equal("Rock", value.Value);
        Assert.Equal(
            [first.Id, second.Id],
            ctx.ItemValuesMap.Where(e => e.ItemValueId.Equals(value.ItemValueId)).Select(e => e.ItemId).OrderBy(e => e).ToArray());
    }

    [Fact]
    public void SaveItems_GenresDifferingOnlyByNulOnOneItem_CreateOneValue()
    {
        var item = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), genres: ["Jazz", "Jazz\0"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("Jazz", Assert.Single(ctx.ItemValues).Value);
        Assert.Single(ctx.ItemValuesMap);
    }

    [Fact]
    public void SaveItems_GenreWithNulSavedAgain_KeepsTheStoredValue()
    {
        var item = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), genres: ["Blues\0"]);
        _service.SaveItems([item], CancellationToken.None);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("Blues", Assert.Single(ctx.ItemValues).Value);
        Assert.Single(ctx.ItemValuesMap);
    }

    [Fact]
    public void SaveItems_GenreOfOnlyNul_IsNotStored()
    {
        var item = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), genres: ["\0", "Jazz"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("Jazz", Assert.Single(ctx.ItemValues).Value);
    }

    [Fact]
    public void SaveItems_NameWithNul_StoresTheCleanNameAndCleanValueWithoutIt()
    {
        var item = CreateBook(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), name: "Mid\0dle", genres: ["Mid\0dle"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        var entity = Assert.Single(ctx.BaseItems.Where(e => e.Id.Equals(item.Id)));
        Assert.Equal("middle", entity.CleanName);
        Assert.Equal("middle", Assert.Single(ctx.ItemValues).CleanValue);
    }

    [Fact]
    public void SaveItems_ProviderIdsDifferingOnlyByNul_KeepOneProvider()
    {
        var item = CreateBook(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            providerIds: new() { ["Imdb"] = "tt0001", ["Imdb\0"] = "tt0002" });

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("Imdb", Assert.Single(ctx.BaseItemProviders.Where(e => e.ItemId.Equals(item.Id))).ProviderId);
    }

    [Fact]
    public async Task ReattachUserData_KeyWithNul_ReattachesTheRowsStoredUnderTheCleanedKey()
    {
        var movie = CreateMovie(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "tt0001\0");
        SeedUserDataItem(movie);
        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(CreateDetachedRow("tt0001"));
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(
                new[] { "tt0001", movie.Id.ToString() }.Order(StringComparer.Ordinal),
                ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).Select(e => e.CustomDataKey).ToArray().Order(StringComparer.Ordinal));
            Assert.Empty(ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
        }
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task ReattachUserData_StoredRowKeyWithLegacyNul_IsCarriedOverWithoutDuplicatingTheCleanedKey()
    {
        var movie = CreateMovie(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "tt0001");
        SeedUserDataItem(movie);
        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(CreateRow(movie.Id, "legacy"));
            ctx.UserData.Add(CreateDetachedRow(movie.Id.ToString()));
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Written around the save hook, as rows saved by an earlier version were.
            await ctx.UserData
                .Where(e => e.CustomDataKey == "legacy")
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.CustomDataKey, "tt0001\0"), TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(
                new[] { "tt0001", movie.Id.ToString() }.Order(StringComparer.Ordinal),
                ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).Select(e => e.CustomDataKey).ToArray().Order(StringComparer.Ordinal));
        }
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task ReattachUserData_DetachedRowKeyedWithNul_IsReattachedWhenTheItemReportsTheSameKey()
    {
        // A file that is moved or replaced is added again with the same metadata, so it reports the same key.
        var movie = CreateMovie(Guid.Parse("12121212-1212-1212-1212-121212121212"), "tt0001\0");
        SeedUserDataItem(movie);
        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(CreateDetachedRow("tt0001\0"));
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(
                new[] { "tt0001", movie.Id.ToString() }.Order(StringComparer.Ordinal),
                ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).Select(e => e.CustomDataKey).ToArray().Order(StringComparer.Ordinal));
            Assert.Empty(ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
        }
    }

    [Fact]
    public void SaveUserData_KeyWithNulSavedTwice_UpdatesTheStoredRow()
    {
        var movie = CreateMovie(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), "tt0001\0");
        SeedUserDataItem(movie);
        var manager = CreateUserDataManager();
        var user = new User("user", "auth-provider", "reset-provider") { Id = _userId };

        manager.SaveUserData(user, movie, new UserItemData { Key = "ignored", PlayCount = 1 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);
        manager.SaveUserData(user, movie, new UserItemData { Key = "ignored", PlayCount = 2 }, UserDataSaveReason.PlaybackFinished, CancellationToken.None);

        using var ctx = CreateDbContext();
        var rows = ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).ToList();
        Assert.Equal(
            new[] { "tt0001", movie.Id.ToString() }.Order(StringComparer.Ordinal),
            rows.Select(e => e.CustomDataKey).Order(StringComparer.Ordinal));
        Assert.All(rows, row => Assert.Equal(2, row.PlayCount));
    }

    [Fact]
    public void GetUserData_KeyWithNul_ReturnsTheRowStoredUnderTheCleanedKey()
    {
        var movie = CreateMovie(Guid.Parse("99999999-9999-9999-9999-999999999999"), "tt0001\0");
        movie.UserData = [CreateRow(movie.Id, movie.Id.ToString(), playCount: 1), CreateRow(movie.Id, "tt0001", playCount: 5)];
        var user = new User("user", "auth-provider", "reset-provider") { Id = _userId };

        var userData = CreateUserDataManager().GetUserData(user, movie);

        Assert.Equal(5, userData?.PlayCount);
    }

    private static Book CreateBook(Guid id, string name = "Book", string[]? genres = null, Dictionary<string, string>? providerIds = null)
    {
        return new Book
        {
            Id = id,
            Name = name,
            Genres = genres ?? [],
            ProviderIds = providerIds ?? []
        };
    }

    private static Movie CreateMovie(Guid id, string imdbId)
    {
        return new Movie
        {
            Id = id,
            Name = "Black Widow",
            ProviderIds = new Dictionary<string, string> { ["Imdb"] = imdbId }
        };
    }

    private UserDataManager CreateUserDataManager()
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        return new UserDataManager(configurationManager.Object, CreateDbContextFactory());
    }

    private void SeedUserDataItem(BaseItem item)
    {
        using var ctx = CreateDbContext();
        if (!ctx.Users.Any(e => e.Id.Equals(_userId)))
        {
            ctx.Users.Add(new User("user", "auth-provider", "reset-provider") { Id = _userId });
        }

        if (!ctx.BaseItems.Any(e => e.Id.Equals(BaseItemRepository.PlaceholderId)))
        {
            ctx.BaseItems.Add(new BaseItemEntity { Id = BaseItemRepository.PlaceholderId, Type = typeof(Folder).FullName! });
        }

        ctx.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = item.GetType().FullName! });
        ctx.SaveChanges();
    }

    private UserData CreateDetachedRow(string key)
    {
        var row = CreateRow(BaseItemRepository.PlaceholderId, key);
        row.RetentionDate = new DateTime(2025, 6, 22, 0, 0, 0, DateTimeKind.Utc);
        return row;
    }

    private UserData CreateRow(Guid itemId, string key, int playCount = 1)
    {
        return new UserData
        {
            ItemId = itemId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = key,
            PlayCount = playCount
        };
    }
}
