using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceOwnedRowTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ItemPersistenceOwnedRowTests()
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

        // Video.SourceType consults this before it can produce user data keys.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
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

    [Fact]
    public void SaveItems_UpdateExistingItem_ReplacesOwnedRows()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        _service.SaveItems(
            [CreateBook(id, new() { ["Imdb"] = "tt0001", ["Tmdb"] = "555" }, [MetadataField.Name])],
            CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(2, ctx.BaseItemProviders.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(1, ctx.BaseItemImageInfos.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(1, ctx.BaseItemMetadataFields.Count(e => e.ItemId.Equals(id)));
        }

        // Re-save with different owned rows: the update path rewrites all three tables wholesale.
        _service.SaveItems(
            [CreateBook(id, new() { ["Imdb"] = "tt9999" }, [MetadataField.Name, MetadataField.Genres])],
            CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            var providers = ctx.BaseItemProviders.Where(e => e.ItemId.Equals(id)).ToList();
            Assert.Equal("tt9999", Assert.Single(providers).ProviderValue);

            Assert.Equal(1, ctx.BaseItemImageInfos.Count(e => e.ItemId.Equals(id)));
            Assert.Equal(2, ctx.BaseItemMetadataFields.Count(e => e.ItemId.Equals(id)));
        }
    }

    [Fact]
    public void SaveItems_MixedNewAndExistingBatch_ReplacesOnlyExistingOwnedRows()
    {
        var existing = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var fresh = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        _service.SaveItems([CreateBook(existing, new() { ["Imdb"] = "tt0001" }, [])], CancellationToken.None);

        // One already-persisted item and one brand new item in the same batch.
        _service.SaveItems(
            [
                CreateBook(existing, new() { ["Imdb"] = "tt0002" }, []),
                CreateBook(fresh, new() { ["Tmdb"] = "777" }, [])
            ],
            CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal("tt0002", Assert.Single(ctx.BaseItemProviders.Where(e => e.ItemId.Equals(existing))).ProviderValue);
        Assert.Equal("777", Assert.Single(ctx.BaseItemProviders.Where(e => e.ItemId.Equals(fresh))).ProviderValue);
    }

    [Fact]
    public async Task ReattachUserData_DetachedRowsFromDifferentEras_CollapsesToMostRecentPlay()
    {
        var movie = CreateMovie(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var keys = movie.GetUserDataKeys();
        SeedUserDataItem(movie);

        using (var ctx = CreateDbContext())
        {
            // The guid-keyed row was detached by an older deletion than the provider-keyed ones.
            ctx.UserData.AddRange(
                CreateDetachedRow(keys[^1], new DateTime(2021, 12, 31, 0, 0, 0, DateTimeKind.Utc), playCount: 7, positionTicks: 490),
                CreateDetachedRow(keys[0], new DateTime(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc), playCount: 9, positionTicks: 0, played: true),
                CreateDetachedRow(keys[1], new DateTime(2023, 8, 14, 0, 0, 0, DateTimeKind.Utc), playCount: 9, positionTicks: 0, played: true));
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var rows = ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)).ToList();

            Assert.Equal(keys.Count, rows.Count);
            Assert.Equal(keys.OrderBy(e => e, StringComparer.Ordinal), rows.Select(e => e.CustomDataKey).OrderBy(e => e, StringComparer.Ordinal));
            Assert.All(rows, row =>
            {
                Assert.True(row.Played);
                Assert.Equal(0, row.PlaybackPositionTicks);
                Assert.Equal(9, row.PlayCount);
                Assert.Null(row.RetentionDate);
            });

            Assert.Empty(ctx.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId)));
        }
    }

    [Fact]
    public async Task ReattachUserData_NoDetachedRows_LeavesExistingRowsAlone()
    {
        var movie = CreateMovie(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var keys = movie.GetUserDataKeys();
        SeedUserDataItem(movie);

        using (var ctx = CreateDbContext())
        {
            ctx.UserData.Add(CreateRow(movie.Id, keys[0], new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), playCount: 1, positionTicks: 123));
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await _service.ReattachUserDataAsync(movie, TestContext.Current.CancellationToken);

        using (var ctx = CreateDbContext())
        {
            var row = Assert.Single(ctx.UserData.Where(e => e.ItemId.Equals(movie.Id)));
            Assert.Equal(123, row.PlaybackPositionTicks);
        }
    }

    private static Book CreateBook(Guid id, Dictionary<string, string> providerIds, MetadataField[] lockedFields)
    {
        var book = new Book
        {
            Id = id,
            Name = "Book",
            ProviderIds = providerIds,
            LockedFields = lockedFields
        };

        book.SetImage(new ItemImageInfo { Path = "/img/primary.jpg", Type = ImageType.Primary }, 0);
        return book;
    }

    private static Movie CreateMovie(Guid id)
    {
        return new Movie
        {
            Id = id,
            Name = "Black Widow",
            ProviderIds = new Dictionary<string, string>
            {
                ["Tmdb"] = "497698",
                ["Imdb"] = "tt3480822"
            }
        };
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

    private UserData CreateDetachedRow(string key, DateTime lastPlayed, int playCount, long positionTicks, bool played = false)
    {
        var row = CreateRow(BaseItemRepository.PlaceholderId, key, lastPlayed, playCount, positionTicks, played);
        row.RetentionDate = new DateTime(2025, 6, 22, 0, 0, 0, DateTimeKind.Utc);

        return row;
    }

    private UserData CreateRow(Guid itemId, string key, DateTime lastPlayed, int playCount, long positionTicks, bool played = false)
    {
        return new UserData
        {
            ItemId = itemId,
            Item = null,
            UserId = _userId,
            User = null,
            CustomDataKey = key,
            LastPlayedDate = lastPlayed,
            PlayCount = playCount,
            PlaybackPositionTicks = positionTicks,
            Played = played
        };
    }
}
