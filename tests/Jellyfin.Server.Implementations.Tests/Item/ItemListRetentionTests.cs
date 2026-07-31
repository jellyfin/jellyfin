using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Verifies retention and reattachment of detached user-list entries.
/// </summary>
public sealed class ItemListRetentionTests : IDisposable
{
    private const string ExpiredKey = "expired-key";
    private const string FreshKey = "fresh-key";
    private const string NullRetentionKey = "null-retention-key";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IDbContextFactory<JellyfinDbContext> _factory;
    private readonly Dictionary<Guid, BaseItem> _libraryItems = new();
    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly ItemPersistenceService _itemPersistenceService;
    private readonly ItemListManager _itemListManager;

    public ItemListRetentionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(provider => provider.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(provider => provider.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);
        _factory = factory.Object;

        _libraryManager = new Mock<ILibraryManager>();
        _libraryManager
            .Setup(manager => manager.GetItemById(It.IsAny<Guid>()))
            .Returns((Guid itemId) => _libraryItems.GetValueOrDefault(itemId));
        _libraryManager
            .Setup(manager => manager.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = _libraryManager.Object;
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(manager => manager.Configuration).Returns(new ServerConfiguration());

        // BaseItem.CreateSortName reads the static configuration manager when an item is persisted.
        BaseItem.ConfigurationManager = configurationManager.Object;

        _itemListManager = new ItemListManager(
            configurationManager.Object,
            _factory,
            new Lazy<ILibraryManager>(() => _libraryManager.Object));

        _itemPersistenceService = new ItemPersistenceService(
            _factory,
            new Mock<IServerApplicationHost>().Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    [Fact]
    public async Task DeleteAndReattach_ReplacementWithSameProviderId_SurvivesWithNewItemId()
    {
        const string ImdbId = "tt1234567";
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = SeedUser();
        var originalItem = CreateMovie("Original", ImdbId);
        SaveItem(originalItem, cancellationToken);
        var list = await _itemListManager.GetOrCreateDefaultListAsync(userId);
        await _itemListManager.AddItemAsync(list.Id, originalItem.Id);

        _itemPersistenceService.DeleteItem(originalItem.Id);

        using (var context = CreateDbContext())
        {
            var detachedEntry = await context.ItemListBaseItemMap
                .SingleAsync(
                    entry => entry.ItemListId.Equals(list.Id) && entry.CustomDataKey == ImdbId,
                    cancellationToken);
            Assert.False(detachedEntry.ItemId.HasValue);
            Assert.NotNull(detachedEntry.RetentionDate);
        }

        var replacementItem = CreateMovie("Replacement", ImdbId);
        Assert.NotEqual(originalItem.Id, replacementItem.Id);
        SaveItem(replacementItem, cancellationToken);

        await _itemPersistenceService.ReattachUserDataAsync(replacementItem, cancellationToken);

        using (var context = CreateDbContext())
        {
            var reattachedEntry = await context.ItemListBaseItemMap
                .SingleAsync(
                    entry => entry.ItemListId.Equals(list.Id) && entry.CustomDataKey == ImdbId,
                    cancellationToken);
            Assert.True(reattachedEntry.ItemId.HasValue);
            Assert.Equal(replacementItem.Id, reattachedEntry.ItemId!.Value);
            Assert.Null(reattachedEntry.RetentionDate);
            Assert.False(await context.BaseItems.AnyAsync(
                item => item.Id.Equals(originalItem.Id),
                cancellationToken));
            Assert.True(await context.BaseItems.AnyAsync(
                item => item.Id.Equals(replacementItem.Id),
                cancellationToken));
        }
    }

    [Fact]
    public async Task DeleteItems_SharedCustomDataKey_DetachesSingleListEntryWithoutCollision()
    {
        const string ImdbId = "tt7654321";
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = SeedUser();
        var firstItem = CreateMovie("First", ImdbId);
        var secondItem = CreateMovie("Second", ImdbId);
        Assert.Equal(firstItem.GetUserDataKeys().First(), secondItem.GetUserDataKeys().First());
        Assert.Equal(ImdbId, firstItem.GetUserDataKeys().First());
        SaveItem(firstItem, cancellationToken);
        SaveItem(secondItem, cancellationToken);
        var list = await _itemListManager.GetOrCreateDefaultListAsync(userId);

        await _itemListManager.AddItemAsync(list.Id, firstItem.Id);
        await _itemListManager.AddItemAsync(list.Id, secondItem.Id);

        using (var context = CreateDbContext())
        {
            var deduplicatedEntry = await context.ItemListBaseItemMap.SingleAsync(
                entry => entry.ItemListId.Equals(list.Id),
                cancellationToken);
            Assert.Equal(ImdbId, deduplicatedEntry.CustomDataKey);
            Assert.Equal(firstItem.Id, deduplicatedEntry.ItemId);
        }

        _itemPersistenceService.DeleteItem(firstItem.Id, secondItem.Id);

        using (var context = CreateDbContext())
        {
            var detachedEntry = await context.ItemListBaseItemMap.SingleAsync(
                entry => entry.ItemListId.Equals(list.Id),
                cancellationToken);
            Assert.Equal(ImdbId, detachedEntry.CustomDataKey);
            Assert.False(detachedEntry.ItemId.HasValue);
            Assert.NotNull(detachedEntry.RetentionDate);
            Assert.False(await context.BaseItems.AnyAsync(
                item => item.Id.Equals(firstItem.Id) || item.Id.Equals(secondItem.Id),
                cancellationToken));
        }
    }

    [Fact]
    public async Task ReattachUserDataAsync_ItemWithoutProviderIds_RemainsDetached()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userId = SeedUser();
        var originalItem = CreateMovie("Original without provider");
        SaveItem(originalItem, cancellationToken);
        var list = await _itemListManager.GetOrCreateDefaultListAsync(userId);
        await _itemListManager.AddItemAsync(list.Id, originalItem.Id);

        _itemPersistenceService.DeleteItem(originalItem.Id);

        var replacementItem = CreateMovie("Replacement without provider");
        Assert.NotEqual(originalItem.Id, replacementItem.Id);
        Assert.NotEqual(originalItem.GetUserDataKeys().First(), replacementItem.GetUserDataKeys().First());
        SaveItem(replacementItem, cancellationToken);
        await _itemPersistenceService.ReattachUserDataAsync(replacementItem, cancellationToken);

        using var context = CreateDbContext();
        var detachedEntry = await context.ItemListBaseItemMap.SingleAsync(
            entry => entry.ItemListId.Equals(list.Id),
            cancellationToken);
        Assert.Equal(originalItem.Id.ToString(), detachedEntry.CustomDataKey);
        Assert.False(detachedEntry.ItemId.HasValue);
        Assert.NotNull(detachedEntry.RetentionDate);
        Assert.True(await context.BaseItems.AnyAsync(
            item => item.Id.Equals(replacementItem.Id),
            cancellationToken));
    }

    [Fact]
    public async Task CleanupUserDataTask_ReapsOnlyExpiredDetachedListEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var listId = SeedDetachedEntries();
        var progress = new Mock<IProgress<double>>();
        var task = new CleanupUserDataTask(
            new Mock<ILocalizationManager>().Object,
            _factory,
            NullLogger<CleanupUserDataTask>.Instance);

        await task.ExecuteAsync(progress.Object, cancellationToken);

        using var context = CreateDbContext();
        var retainedEntries = await context.ItemListBaseItemMap
            .Where(entry => entry.ItemListId.Equals(listId))
            .OrderBy(entry => entry.SortIndex)
            .ToListAsync(cancellationToken);
        Assert.DoesNotContain(retainedEntries, entry => entry.CustomDataKey == ExpiredKey);
        Assert.Collection(
            retainedEntries,
            entry =>
            {
                Assert.Equal(FreshKey, entry.CustomDataKey);
                Assert.NotNull(entry.RetentionDate);
                Assert.False(entry.ItemId.HasValue);
            },
            entry =>
            {
                Assert.Equal(NullRetentionKey, entry.CustomDataKey);
                Assert.Null(entry.RetentionDate);
                Assert.False(entry.ItemId.HasValue);
            });
        progress.Verify(reporter => reporter.Report(100), Times.Once);
    }

    public void Dispose()
    {
        _itemListManager.Dispose();
        _connection.Dispose();
    }

    private Movie CreateMovie(string name, string? imdbId = null)
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = name
        };
        if (imdbId is not null)
        {
            movie.SetProviderId(MetadataProvider.Imdb, imdbId);
        }

        _libraryItems.Add(movie.Id, movie);
        return movie;
    }

    private void SaveItem(Movie item, CancellationToken cancellationToken)
    {
        _itemPersistenceService.SaveItems([item], cancellationToken);
    }

    private Guid SeedUser()
    {
        var user = new User(
            "retention-user-" + Guid.NewGuid().ToString("N"),
            "authentication",
            "password-reset");
        using var context = CreateDbContext();
        context.Users.Add(user);
        context.SaveChanges();
        return user.Id;
    }

    private Guid SeedDetachedEntries()
    {
        using var context = CreateDbContext();
        var user = new User(
            "reaper-user-" + Guid.NewGuid().ToString("N"),
            "authentication",
            "password-reset");
        var list = new ItemList
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Watchlist",
            ListType = ItemListType.Watchlist,
            IsDefault = true,
            AutoRemoveWatched = true,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.ItemLists.Add(list);
        context.ItemListBaseItemMap.AddRange(
            CreateDetachedEntry(list, ExpiredKey, DateTime.UtcNow.AddDays(-91), 0),
            CreateDetachedEntry(list, FreshKey, DateTime.UtcNow.AddDays(-89), 1),
            CreateDetachedEntry(list, NullRetentionKey, null, 2));
        context.SaveChanges();
        return list.Id;
    }

    private static ItemListBaseItemMap CreateDetachedEntry(
        ItemList list,
        string customDataKey,
        DateTime? retentionDate,
        int sortIndex)
    {
        return new ItemListBaseItemMap
        {
            ItemListId = list.Id,
            ItemList = list,
            CustomDataKey = customDataKey,
            ItemId = null,
            Item = null,
            DateAdded = new DateTime(2026, 1, 1),
            RetentionDate = retentionDate,
            SortIndex = sortIndex
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
