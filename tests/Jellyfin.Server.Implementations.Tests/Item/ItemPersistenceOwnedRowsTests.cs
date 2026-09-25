using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the rows an item owns outright — its images, provider ids and locked fields. Saving an
/// item rewrites all three wholesale from what the instance holds, so an item read without them
/// carries an empty collection that means "not read", not "none". Telling those two apart is the
/// only thing standing between a cheap read and silently deleting the rows on the next save.
/// </summary>
public sealed class ItemPersistenceOwnedRowsTests : SqliteDbTestFixture
{
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001");

    private readonly ItemPersistenceService _service;
    private readonly BaseItemRepository _repository;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemPersistenceOwnedRowsTests"/> class.
    /// </summary>
    public ItemPersistenceOwnedRowsTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        // Video.SourceType asks this whether the file is an in-progress recording.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);

        var lookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(lookup);
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            appHost.Object,
            NullLogger<ItemPersistenceService>.Instance);

        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _movieId,
            Type = lookup.BaseItemKindNames[BaseItemKind.Movie]!,
            Name = "Movie",
            Path = "/movies/movie.mkv",
            PresentationUniqueKey = _movieId.ToString("N")
        });
        context.SaveChanges();

        context.BaseItemImageInfos.Add(new BaseItemImageInfo
        {
            Id = Guid.NewGuid(),
            ItemId = _movieId,
            ImageType = ImageInfoImageType.Primary,
            Path = "/movies/poster.jpg",
            Width = 100,
            Height = 150,
            Item = null!
        });
        context.BaseItemProviders.Add(new BaseItemProvider
        {
            ItemId = _movieId,
            ProviderId = "Tmdb",
            ProviderValue = "603",
            Item = null!
        });
        context.BaseItemMetadataFields.Add(new BaseItemMetadataField
        {
            Id = (int)MetadataField.Name,
            ItemId = _movieId,
            Item = null!
        });
        context.SaveChanges();
    }

    /// <summary>
    /// An item read with every field still carries its rows, so saving it round-trips them.
    /// </summary>
    [Fact]
    public void SaveItems_ItemReadWithAllFields_KeepsItsOwnedRows()
    {
        var item = Read(new DtoOptions());

        Assert.Single(item.ImageInfos);
        Assert.Single(item.ProviderIds);
        Assert.Single(item.LockedFields);

        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 1, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// The one that matters: a cheap read asks for none of them, so the instance holds nothing.
    /// Saving it must leave the stored rows alone rather than take the empty collections as the
    /// item's new state.
    /// </summary>
    [Fact]
    public void SaveItems_ItemReadWithoutOwnedRows_DoesNotDeleteThem()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);

        Assert.Empty(item.ImageInfos);
        Assert.Empty(item.ProviderIds);
        Assert.Empty(item.LockedFields);

        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 1, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// An entry added to a set the item never read is merged in. Rewriting from it would keep the
    /// new entry and drop every stored one; refusing it would lose the entry instead.
    /// </summary>
    [Fact]
    public void SaveItems_CollectionFilledInOnAnUnreadItem_IsMergedIn()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.ProviderIds["Imdb"] = "tt0111161";

        _service.SaveItems([item], CancellationToken.None);

        Assert.Equal(
            new Dictionary<string, string> { ["Imdb"] = "tt0111161", ["Tmdb"] = "603" },
            StoredProviders(_movieId));
    }

    /// <summary>
    /// Assigning a whole collection does not make it read, because the value is usually derived from
    /// the unread one. It is merged, matching the stored key regardless of case.
    /// </summary>
    [Fact]
    public void SaveItems_CollectionAssignedOnAnUnreadItem_IsMergedIn()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["tmdb"] = "604" };

        _service.SaveItems([item], CancellationToken.None);

        Assert.Equal(new Dictionary<string, string> { ["Tmdb"] = "604" }, StoredProviders(_movieId));
    }

    /// <summary>
    /// AddImage builds a new array from the unread empty one. Saving it must add the image, not
    /// replace the stored ones with it - and saving again must not add it twice.
    /// </summary>
    [Fact]
    public void SaveItems_ImageAddedToAnUnreadItem_KeepsTheStoredOnes()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.AddImage(new ItemImageInfo { Path = "/movies/backdrop.jpg", Type = ImageType.Backdrop });

        _service.SaveItems([item], CancellationToken.None);
        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 2, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// Locking a field on an item that never read its locks adds to the stored ones.
    /// </summary>
    [Fact]
    public void SaveItems_FieldLockedOnAnUnreadItem_KeepsTheStoredLocks()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.LockedFields = [MetadataField.Overview];

        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 1, providers: 1, lockedFields: 2);
    }

    /// <summary>
    /// A video hands its image objects, row ids included, to its alternate versions. Each item must
    /// still get rows of its own rather than colliding with the other's.
    /// </summary>
    [Fact]
    public void SaveItems_ImagesSharedWithAnotherItem_GiveEachItsOwnRows()
    {
        var other = new Movie { Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000003"), Name = "Other", Path = "/movies/other.mkv" };
        _service.SaveItems([other], CancellationToken.None);

        other.ImageInfos = Read(new DtoOptions()).ImageInfos;
        _service.SaveItems([other], CancellationToken.None);

        using var context = CreateDbContext();
        var original = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)));
        var copy = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(other.Id)));
        Assert.NotEqual(original.Id, copy.Id);
    }

    /// <summary>
    /// Once a new item is inserted it holds exactly what is stored. Providers fill its ids in place
    /// after that first save, and the next one must still write them.
    /// </summary>
    [Fact]
    public void SaveItems_NewItemGivenProviderIdsInPlace_WritesThem()
    {
        var item = new Movie
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000002"),
            Name = "New Movie",
            Path = "/movies/new.mkv"
        };
        _service.SaveItems([item], CancellationToken.None);

        item.TrySetProviderId(MetadataProvider.Tmdb, "550");
        _service.SaveItems([item], CancellationToken.None);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemProviders.Where(e => e.ItemId.Equals(item.Id)));
        Assert.Equal("550", stored.ProviderValue);
    }

    /// <summary>
    /// A new instance built for an id that is already stored knows nothing of that id's rows, so
    /// saving it must not take its empty collections as the item's state - a user's locked fields
    /// included.
    /// </summary>
    [Fact]
    public void SaveItems_NewInstanceForAStoredId_DoesNotDeleteItsRows()
    {
        var item = new Movie { Id = _movieId, Name = "Movie", Path = "/movies/movie.mkv" };

        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 1, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// Nor may it swap in the one id it was given for the stored set; the id is merged in.
    /// </summary>
    [Fact]
    public void SaveItems_NewInstanceForAStoredIdGivenProviderIds_MergesThem()
    {
        var item = new Movie { Id = _movieId, Name = "Movie", Path = "/movies/movie.mkv" };
        item.TrySetProviderId(MetadataProvider.Imdb, "tt0111161");

        _service.SaveItems([item], CancellationToken.None);

        Assert.Equal(
            new Dictionary<string, string> { ["Imdb"] = "tt0111161", ["Tmdb"] = "603" },
            StoredProviders(_movieId));
    }

    /// <summary>
    /// SaveImagesAsync replaces the stored images outright, so it needs the same guard as SaveItems:
    /// a scan reaches it through ValidateChildren with children that may not carry their images.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveImagesAsync_ItemReadWithoutImages_DoesNotDeleteThem()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);

        await _service.SaveImagesAsync(item, CancellationToken.None).ConfigureAwait(true);

        AssertStoredCounts(images: 1, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// An image a refresh added to an item read without its images is merged in by SaveImagesAsync too.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveImagesAsync_ImageAddedToAnUnreadItem_IsMergedIn()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.AddImage(new ItemImageInfo { Path = "/movies/backdrop.jpg", Type = ImageType.Backdrop });

        await _service.SaveImagesAsync(item, CancellationToken.None).ConfigureAwait(true);

        AssertStoredCounts(images: 2, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// An item that did read its images still writes them.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SaveImagesAsync_ItemReadWithImages_ReplacesThem()
    {
        var item = Read(new DtoOptions());
        item.ImageInfos = [new ItemImageInfo { Path = "/movies/new.jpg", Type = ImageType.Backdrop }];

        await _service.SaveImagesAsync(item, CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal("/movies/new.jpg", stored.Path);
    }

    /// <summary>
    /// Clearing a collection on an item that was read still clears the stored rows.
    /// </summary>
    [Fact]
    public void SaveItems_ImagesClearedOnAReadItem_DeletesThem()
    {
        var item = Read(new DtoOptions());
        item.ImageInfos = [];

        _service.SaveItems([item], CancellationToken.None);

        AssertStoredCounts(images: 0, providers: 1, lockedFields: 1);
    }

    /// <summary>
    /// The way to change one provider id without holding the rest: the others survive.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpsertProviderIdAsync_NewProvider_LeavesTheOthersAlone()
    {
        await _service.UpsertProviderIdAsync(_movieId, "Imdb", "tt0111161", CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var providers = context.BaseItemProviders
            .Where(e => e.ItemId.Equals(_movieId))
            .ToDictionary(e => e.ProviderId, e => e.ProviderValue);

        Assert.Equal(2, providers.Count);
        Assert.Equal("603", providers["Tmdb"]);
        Assert.Equal("tt0111161", providers["Imdb"]);
    }

    /// <summary>
    /// Writing one that already exists updates it rather than failing on the primary key.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpsertProviderIdAsync_ExistingProvider_UpdatesTheValue()
    {
        await _service.UpsertProviderIdAsync(_movieId, "Tmdb", "604", CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemProviders.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal("604", stored.ProviderValue);
    }

    /// <summary>
    /// Removing one leaves the rest.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveProviderIdAsync_LeavesTheOthersAlone()
    {
        await _service.UpsertProviderIdAsync(_movieId, "Imdb", "tt0111161", CancellationToken.None).ConfigureAwait(true);
        await _service.RemoveProviderIdAsync(_movieId, "Tmdb", CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemProviders.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal("Imdb", stored.ProviderId);
    }

    /// <summary>
    /// An image read from the database keeps its row across a save, so a save no longer renames
    /// every image it rewrites — and there is something stable to target a write at.
    /// </summary>
    [Fact]
    public void SaveItems_RoundTrip_KeepsTheImageRowIdentity()
    {
        var before = Read(new DtoOptions()).ImageInfos.Single().Id;
        Assert.NotEqual(Guid.Empty, before);

        _service.SaveItems([Read(new DtoOptions())], CancellationToken.None);

        Assert.Equal(before, Read(new DtoOptions()).ImageInfos.Single().Id);
    }

    /// <summary>
    /// Adding one image leaves the item's other images alone.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpsertImageAsync_NewImage_LeavesTheOthersAlone()
    {
        var added = new ItemImageInfo { Path = "/movies/backdrop.jpg", Type = ImageType.Backdrop };

        await _service.UpsertImageAsync(_movieId, added, CancellationToken.None).ConfigureAwait(true);

        Assert.NotEqual(Guid.Empty, added.Id);
        using var context = CreateDbContext();
        var stored = context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)).ToList();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, e => e.Path == "/movies/poster.jpg");
        Assert.Contains(stored, e => e.Path == "/movies/backdrop.jpg");
    }

    /// <summary>
    /// Writing an image that is already stored updates its row rather than adding a duplicate.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpsertImageAsync_ExistingImage_UpdatesItInPlace()
    {
        var image = Read(new DtoOptions()).ImageInfos.Single();
        var originalId = image.Id;
        image.Width = 640;

        await _service.UpsertImageAsync(_movieId, image, CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(originalId, stored.Id);
        Assert.Equal(640, stored.Width);
    }

    /// <summary>
    /// An image with no id yet is matched on the type and path that identify it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UpsertImageAsync_UnstoredImageMatchingByPath_DoesNotDuplicate()
    {
        var image = new ItemImageInfo { Path = "/movies/poster.jpg", Type = ImageType.Primary, Height = 999 };

        await _service.UpsertImageAsync(_movieId, image, CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(999, stored.Height);
    }

    /// <summary>
    /// Removing one image leaves the rest.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RemoveImageAsync_LeavesTheOthersAlone()
    {
        var added = new ItemImageInfo { Path = "/movies/backdrop.jpg", Type = ImageType.Backdrop };
        await _service.UpsertImageAsync(_movieId, added, CancellationToken.None).ConfigureAwait(true);

        await _service.RemoveImageAsync(_movieId, added, CancellationToken.None).ConfigureAwait(true);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemImageInfos.Where(e => e.ItemId.Equals(_movieId)));
        Assert.Equal("/movies/poster.jpg", stored.Path);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    private Movie Read(DtoOptions options)
        => _repository.GetItemList(new InternalItemsQuery
        {
            ItemIds = [_movieId],
            DtoOptions = options
        }).OfType<Movie>().Single();

    private Dictionary<string, string> StoredProviders(Guid itemId)
    {
        using var context = CreateDbContext();
        return context.BaseItemProviders
            .Where(e => e.ItemId.Equals(itemId))
            .OrderBy(e => e.ProviderId)
            .ToDictionary(e => e.ProviderId, e => e.ProviderValue);
    }

    private void AssertStoredCounts(int images, int providers, int lockedFields)
    {
        using var context = CreateDbContext();
        Assert.Equal(images, context.BaseItemImageInfos.Count(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(providers, context.BaseItemProviders.Count(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(lockedFields, context.BaseItemMetadataFields.Count(e => e.ItemId.Equals(_movieId)));
    }
}
