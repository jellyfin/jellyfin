using System;
using System.Linq;
using System.Threading;
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
    /// A collection someone actually filled in is written even if the read skipped it — the guard
    /// distinguishes "not read" from "read and cleared", and a write is never "not read".
    /// </summary>
    [Fact]
    public void SaveItems_CollectionFilledInOnAnUnreadItem_IsWritten()
    {
        var item = Read(DtoOptions.StoredColumnsOnly);
        item.ProviderIds["Imdb"] = "tt0111161";

        _service.SaveItems([item], CancellationToken.None);

        using var context = CreateDbContext();
        var providers = context.BaseItemProviders.Where(e => e.ItemId.Equals(_movieId)).ToList();
        Assert.Contains(providers, e => string.Equals(e.ProviderId, "Imdb", StringComparison.Ordinal));
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

    private void AssertStoredCounts(int images, int providers, int lockedFields)
    {
        using var context = CreateDbContext();
        Assert.Equal(images, context.BaseItemImageInfos.Count(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(providers, context.BaseItemProviders.Count(e => e.ItemId.Equals(_movieId)));
        Assert.Equal(lockedFields, context.BaseItemMetadataFields.Count(e => e.ItemId.Equals(_movieId)));
    }
}
