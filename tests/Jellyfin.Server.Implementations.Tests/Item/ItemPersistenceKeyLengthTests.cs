using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The value of a genre, studio, tag or similar list is part of the key of the stored value, so a value from a metadata
/// provider is cut to the length the key allows before values are compared with each other and with the stored
/// ones. Values that are the same up to that length are one value. The name of a provider id is part of the key of its
/// row, so an id with a name that is too long is dropped, and the items saved with it are saved.
/// </summary>
public sealed class ItemPersistenceKeyLengthTests : JellyfinDbTestFixture
{
    private const int ValueLength = KeyNormalizer.ItemValueMaxLength;

    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public ItemPersistenceKeyLengthTests()
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

    [Fact]
    public void SaveItems_ValueOverTheBound_IsStoredCut()
    {
        var item = CreateBook(genres: [new string('v', ValueLength + 100)]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        var value = Assert.Single(ctx.ItemValues);
        Assert.Equal(new string('v', ValueLength), value.Value);
        Assert.Equal(new string('v', ValueLength), value.CleanValue);
    }

    [Fact]
    public void SaveItems_ValueAtTheBound_IsStoredWhole()
    {
        var genre = new string('v', ValueLength);
        var item = CreateBook(genres: [genre]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(genre, Assert.Single(ctx.ItemValues).Value);
    }

    [Fact]
    public void SaveItems_ValuesEqualAfterTheCut_AreOneValueWithOneMapping()
    {
        var prefix = new string('v', ValueLength);
        var item = CreateBook(genres: [prefix + "first", prefix + "second"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(prefix, Assert.Single(ctx.ItemValues).Value);
        Assert.Single(ctx.ItemValuesMap);
    }

    [Fact]
    public void SaveItems_ValuesEqualAfterTheCutOnTwoItems_ShareTheValue()
    {
        var prefix = new string('v', ValueLength);
        var first = CreateBook(genres: [prefix + "first"]);
        var second = CreateBook(genres: [prefix + "second"]);

        _service.SaveItems([first], CancellationToken.None);
        _service.SaveItems([second], CancellationToken.None);

        using var ctx = CreateDbContext();
        var value = Assert.Single(ctx.ItemValues);
        Assert.Equal(
            new[] { first.Id, second.Id }.Order(),
            ctx.ItemValuesMap.Where(e => e.ItemValueId.Equals(value.ItemValueId)).Select(e => e.ItemId).Order());
    }

    [Fact]
    public void SaveItems_CutValueSavedAgain_KeepsTheStoredValue()
    {
        var item = CreateBook(genres: [new string('v', ValueLength + 100)]);
        _service.SaveItems([item], CancellationToken.None);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.ItemValues);
        Assert.Single(ctx.ItemValuesMap);
    }

    [Fact]
    public void SaveItems_SurrogatePairAcrossTheBound_IsNotSplit()
    {
        var item = CreateBook(genres: [new string('v', ValueLength - 1) + "\U0001F600" + "tail"]);

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(new string('v', ValueLength - 1), Assert.Single(ctx.ItemValues).Value);
    }

    [Fact]
    public void SaveItems_ProviderIdNameOverTheBound_IsDroppedAndTheOthersAreKept()
    {
        var item = CreateBook(
            genres: [],
            providerIds: new Dictionary<string, string>
            {
                [new string('p', KeyNormalizer.ProviderIdMaxLength + 1)] = "1",
                [new string('q', KeyNormalizer.ProviderIdMaxLength)] = "2",
                ["Imdb"] = "tt1"
            });

        _service.SaveItems([item], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(
            new[] { "Imdb", new string('q', KeyNormalizer.ProviderIdMaxLength) }.Order(StringComparer.Ordinal),
            ctx.BaseItemProviders.Where(e => e.ItemId.Equals(item.Id)).Select(e => e.ProviderId).AsEnumerable().Order(StringComparer.Ordinal));
        Assert.DoesNotContain(item.ProviderIds.Keys, e => e.Length > KeyNormalizer.ProviderIdMaxLength);
    }

    [Fact]
    public void SaveItems_BatchWithOneItemWithAnOverlongProviderId_SavesEveryItem()
    {
        var items = new[]
        {
            CreateBook(genres: [], providerIds: new Dictionary<string, string> { ["Imdb"] = "tt1" }),
            CreateBook(genres: [], providerIds: new Dictionary<string, string> { [new string('p', KeyNormalizer.ProviderIdMaxLength + 1)] = "1" }),
            CreateBook(genres: [], providerIds: new Dictionary<string, string> { ["Tmdb"] = "2" })
        };

        _service.SaveItems(items, CancellationToken.None);

        using var ctx = CreateDbContext();
        var ids = items.Select(e => e.Id).ToList();
        Assert.Equal(ids.Order(), ctx.BaseItems.Where(e => ids.Contains(e.Id)).Select(e => e.Id).AsEnumerable().Order());
        Assert.Equal(["Imdb", "Tmdb"], ctx.BaseItemProviders.Select(e => e.ProviderId).AsEnumerable().Order(StringComparer.Ordinal));
    }

    private static Book CreateBook(string[] genres, Dictionary<string, string>? providerIds = null)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Book",
            Genres = genres
        };

        if (providerIds is not null)
        {
            foreach (var (name, value) in providerIds)
            {
                book.ProviderIds[name] = value;
            }
        }

        return book;
    }
}
