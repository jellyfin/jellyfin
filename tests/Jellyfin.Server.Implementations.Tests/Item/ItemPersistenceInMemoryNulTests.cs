using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
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
/// Saving an item removes NUL characters from the stored copy, and from the item that stays in memory as well.
/// An item that kept its NUL characters would be matched against rows that no longer have them, for example a
/// series looking for its seasons and episodes by its presentation key.
/// </summary>
public sealed class ItemPersistenceInMemoryNulTests : JellyfinDbTestFixture
{
    private const string KeyWithNul = "tt0903747\0-en";
    private const string CleanKey = "tt0903747-en";

    private readonly ItemPersistenceService _service;
    private readonly BaseItemRepository _repository;
    private readonly IServerApplicationHost _appHost;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly User _user = new("test", "auth", "reset");

    public ItemPersistenceInMemoryNulTests()
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

        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        _appHost = appHost.Object;

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            _appHost,
            NullLogger<ItemPersistenceService>.Instance);
        _repository = new BaseItemRepository(
            CreateDbContextFactory(),
            _appHost,
            new ItemTypeLookup(),
            configurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Fact]
    public void SaveItems_SeriesKeyWithNulSavedAgainAfterRefresh_SeasonsAndEpisodesAreFoundThroughTheSeries()
    {
        var series = new Series { Id = Guid.NewGuid(), Name = "Breaking Bad", PresentationUniqueKey = KeyWithNul };
        var season = new Season { Id = Guid.NewGuid(), Name = "Season 1", SeriesId = series.Id };
        var episode = new Episode { Id = Guid.NewGuid(), Name = "Pilot", SeriesId = series.Id };

        SaveSeriesWithChildren(series, season, episode);
        series.PresentationUniqueKey = KeyWithNul;
        SaveSeriesWithChildren(series, season, episode);

        Assert.Equal([season.Id], FindIds(BaseItemKind.Season, series.PresentationUniqueKey));
        Assert.Equal([episode.Id], FindIds(BaseItemKind.Episode, series.PresentationUniqueKey));
    }

    [Fact]
    public void SaveItems_SeriesKeyWithNul_ChildrenCarryTheKeyTheSeriesIsStoredUnder()
    {
        var series = new Series { Id = Guid.NewGuid(), Name = "Breaking Bad", PresentationUniqueKey = KeyWithNul };
        var season = new Season { Id = Guid.NewGuid(), Name = "Season 1", SeriesId = series.Id };
        var episode = new Episode { Id = Guid.NewGuid(), Name = "Pilot", SeriesId = series.Id };

        SaveSeriesWithChildren(series, season, episode);

        Assert.Equal(CleanKey, season.SeriesPresentationUniqueKey);
        Assert.Equal(CleanKey, episode.SeriesPresentationUniqueKey);
        Assert.Equal([series.Id], _repository.GetItemList(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            PresentationUniqueKey = episode.SeriesPresentationUniqueKey
        }).Select(e => e.Id));
    }

    [Fact]
    public void SaveItems_PresentationUniqueKeyWithNul_ItemIsFoundByItsOwnKey()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book", PresentationUniqueKey = KeyWithNul };

        _service.SaveItems([book], CancellationToken.None);

        var found = _repository.GetItemList(new InternalItemsQuery(_user) { PresentationUniqueKey = book.PresentationUniqueKey });
        Assert.Equal([book.Id], found.Select(e => e.Id));
    }

    [Fact]
    public void SaveItems_ExternalIdWithNul_ItemIsFoundByItsOwnExternalId()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book", ExternalId = "ext\0-1" };

        _service.SaveItems([book], CancellationToken.None);

        var found = _repository.GetItemList(new InternalItemsQuery(_user) { ExternalId = book.ExternalId });
        Assert.Equal([book.Id], found.Select(e => e.Id));
    }

    [Fact]
    public void SaveItems_ExternalSeriesIdWithNul_ItemIsFoundByItsOwnExternalSeriesId()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book", ExternalSeriesId = "ser\0-1" };

        _service.SaveItems([book], CancellationToken.None);

        var found = _repository.GetItemList(new InternalItemsQuery(_user) { ExternalSeriesId = book.ExternalSeriesId });
        Assert.Equal([book.Id], found.Select(e => e.Id));
    }

    [Fact]
    public void SaveItems_StringsWithNul_ItemHoldsTheSameValuesAsTheStoredRow()
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Na\0me",
            ForcedSortName = "Sor\0t",
            Path = "/media/bo\0oks/book.epub",
            CustomRating = "Cus\0tom",
            OfficialRating = "Offi\0cial",
            Overview = "Over\0view",
            PreferredMetadataLanguage = "e\0n",
            PreferredMetadataCountryCode = "G\0B",
            PresentationUniqueKey = KeyWithNul,
            OriginalTitle = "Original\0Title",
            Tagline = "Tag\0line",
            ExternalId = "ext\0id",
            ExternalSeriesId = "series\0id",
            Genres = ["Ro\0ck", "Jazz"],
            ProductionLocations = ["Fra\0nce"],
            Studios = ["Studio\0 One"],
            Tags = ["ta\0g"]
        };

        _service.SaveItems([book], CancellationToken.None);

        using var context = CreateDbContext();
        var entity = Assert.Single(context.BaseItems.Where(e => e.Id.Equals(book.Id)));
        Assert.Equal(book.Name, entity.Name);
        Assert.Equal(book.SortName, entity.SortName);
        Assert.Equal(book.ForcedSortName, entity.ForcedSortName);
        Assert.Equal(book.Path, entity.Path);
        Assert.Equal(book.CustomRating, entity.CustomRating);
        Assert.Equal(book.OfficialRating, entity.OfficialRating);
        Assert.Equal(book.Overview, entity.Overview);
        Assert.Equal(book.PreferredMetadataLanguage, entity.PreferredMetadataLanguage);
        Assert.Equal(book.PreferredMetadataCountryCode, entity.PreferredMetadataCountryCode);
        Assert.Equal(book.PresentationUniqueKey, entity.PresentationUniqueKey);
        Assert.Equal(book.OriginalTitle, entity.OriginalTitle);
        Assert.Equal(book.Tagline, entity.Tagline);
        Assert.Equal(book.ExternalId, entity.ExternalId);
        Assert.Equal(book.ExternalSeriesId, entity.ExternalSeriesId);
        Assert.Equal(string.Join('|', book.Genres), entity.Genres);
        Assert.Equal(string.Join('|', book.ProductionLocations), entity.ProductionLocations);
        Assert.Equal(string.Join('|', book.Studios), entity.Studios);
        Assert.Equal(string.Join('|', book.Tags), entity.Tags);
        Assert.DoesNotContain('\0', book.Name);
    }

    [Fact]
    public void SaveItems_ArtistsWithNul_ItemHoldsTheSameValuesAsTheStoredRow()
    {
        var audio = new Audio
        {
            Id = Guid.NewGuid(),
            Name = "Song",
            Artists = ["Art\0ist", "\0"],
            AlbumArtists = ["Album\0 Artist"]
        };

        _service.SaveItems([audio], CancellationToken.None);

        using var context = CreateDbContext();
        var entity = Assert.Single(context.BaseItems.Where(e => e.Id.Equals(audio.Id)));
        Assert.Equal(["Artist"], audio.Artists);
        Assert.Equal(["Album Artist"], audio.AlbumArtists);
        Assert.Equal(string.Join('|', audio.Artists), entity.Artists);
        Assert.Equal(string.Join('|', audio.AlbumArtists), entity.AlbumArtists);
    }

    [Fact]
    public void SaveItems_EntryOfOnlyNul_IsDroppedFromTheItem()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book", Genres = ["Rock", "\0"], Tags = ["\0"] };

        _service.SaveItems([book], CancellationToken.None);

        Assert.Equal(["Rock"], book.Genres);
        Assert.Empty(book.Tags);
    }

    [Fact]
    public void SaveItems_ProviderIdsWithNul_ItemHoldsTheSameValuesAsTheStoredRows()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book" };
        book.ProviderIds["Im\0db"] = "tt01\0";
        book.ProviderIds["Tvdb"] = "42";

        _service.SaveItems([book], CancellationToken.None);

        using var context = CreateDbContext();
        var stored = context.BaseItemProviders.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        Assert.Equal(book.ProviderIds.OrderBy(e => e.Key), stored.OrderBy(e => e.Key));
        Assert.Equal("tt01", book.ProviderIds["Imdb"]);
        Assert.Equal(2, book.ProviderIds.Count);
    }

    [Fact]
    public void SaveItems_ProviderIdOfOnlyNul_IsNeitherStoredNorKeptOnTheItem()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book" };
        book.ProviderIds["Imdb"] = "\0\0";
        book.ProviderIds["\0"] = "tt01";
        book.ProviderIds["Tvdb"] = "42";

        _service.SaveItems([book], CancellationToken.None);

        using var context = CreateDbContext();
        var stored = context.BaseItemProviders.ToDictionary(e => e.ProviderId, e => e.ProviderValue);
        Assert.Equal(new Dictionary<string, string> { ["Tvdb"] = "42" }, stored);
        Assert.Equal(stored.OrderBy(e => e.Key), book.ProviderIds.OrderBy(e => e.Key));
    }

    [Fact]
    public void SaveItems_ProviderIdsDifferingOnlyByNul_ItemKeepsTheEntryThatIsStored()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book" };
        book.ProviderIds["Imdb"] = "tt01";
        book.ProviderIds["Imdb\0"] = "tt02";

        _service.SaveItems([book], CancellationToken.None);

        using var context = CreateDbContext();
        var stored = Assert.Single(context.BaseItemProviders.Where(e => e.ItemId.Equals(book.Id)));
        Assert.Equal(stored.ProviderValue, Assert.Single(book.ProviderIds).Value);
        Assert.Equal(stored.ProviderId, Assert.Single(book.ProviderIds).Key);
    }

    [Fact]
    public void SaveItems_GenresDifferingOnlyByNul_ItemHoldsTheValuesThatAreStored()
    {
        var book = new Book { Id = Guid.NewGuid(), Name = "Book", Genres = ["Rock", "Rock\0", "Jazz"] };

        _service.SaveItems([book], CancellationToken.None);

        using var context = CreateDbContext();
        var entity = Assert.Single(context.BaseItems.Where(e => e.Id.Equals(book.Id)));
        Assert.Equal(["Rock", "Jazz"], book.Genres);
        Assert.Equal(string.Join('|', book.Genres), entity.Genres);
    }

    [Fact]
    public async Task SaveImages_PathWithNul_ItemHoldsTheSamePathAsTheStoredRow()
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = "Book",
            ImageInfos = [new ItemImageInfo { Path = "/images/pri\0mary.jpg", Type = ImageType.Primary }]
        };
        _service.SaveItems([book], CancellationToken.None);
        book.ImageInfos = [new ItemImageInfo { Path = "/images/pri\0mary.jpg", Type = ImageType.Primary }];

        await _service.SaveImagesAsync(book, CancellationToken.None);

        using var context = CreateDbContext();
        var image = Assert.Single(context.BaseItemImageInfos);
        Assert.Equal("/images/primary.jpg", image.Path);
        Assert.Equal(image.Path, Assert.Single(book.ImageInfos).Path);
    }

    [Fact]
    public void SaveItems_StringsOfEveryMappedProperty_LeaveNoNulInTheItemOrTheMappedRow()
    {
        // Fills every writable string property of the item types with a NUL character by reflection, so a
        // string that BaseItemMapper starts copying into a column and that is not cleaned on the item fails here.
        var types = typeof(BaseItem).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseItem)) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) is not null)
            .ToArray();
        Assert.Contains(typeof(Episode), types);
        Assert.Contains(typeof(LiveTvProgram), types);
        var columns = typeof(BaseItemEntity).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .ToArray();
        var mappedBeforeCleaning = new HashSet<string>();
        var stillHoldingNul = new List<string>();

        KnownGap.On(KnownGaps.NulInExactMatchParameter, () =>
        {
            foreach (var type in types)
            {
                var item = CreateItemWithNulInEveryString(type);
                mappedBeforeCleaning.UnionWith(ColumnsWithNul(BaseItemMapper.Map(item, _appHost), columns));

                _service.SaveItems([item], CancellationToken.None);

                var mapped = BaseItemMapper.Map(item, _appHost);
                stillHoldingNul.AddRange(ColumnsWithNul(mapped, columns).Select(c => $"{type.Name}.{c}"));
                stillHoldingNul.AddRange(mapped.Provider!.Where(p => p.ProviderId.Contains('\0', StringComparison.Ordinal)
                        || p.ProviderValue.Contains('\0', StringComparison.Ordinal))
                    .Select(_ => $"{type.Name}.Provider"));
                stillHoldingNul.AddRange(mapped.Images!.Where(i => i.Path.Contains('\0', StringComparison.Ordinal))
                    .Select(_ => $"{type.Name}.Images"));
            }
        });

        // Columns that are not copied from a string of the item, or that are not stored as given.
        var notCopied = new HashSet<string> { "Type", "Data", "MediaType", "UnratedType", "CleanName", "CleanOriginalTitle" };
        var expected = columns.Select(c => c.Name).Where(c => !notCopied.Contains(c)).Order().ToArray();

        Assert.Equal(expected, mappedBeforeCleaning.Order().ToArray());
        Assert.Empty(stillHoldingNul);
    }

    private static string[] ColumnsWithNul(BaseItemEntity entity, PropertyInfo[] columns)
    {
        return columns
            .Where(c => c.Name != nameof(BaseItemEntity.Data) && ((string?)c.GetValue(entity))?.Contains('\0', StringComparison.Ordinal) == true)
            .Select(c => c.Name)
            .ToArray();
    }

    private static BaseItem CreateItemWithNulInEveryString(Type type)
    {
        var item = (BaseItem)Activator.CreateInstance(type)!;
        item.Id = Guid.NewGuid();
        foreach (var property in type.GetProperties().Where(p => p.CanWrite && p.GetSetMethod() is not null))
        {
            var value = "a" + property.Name + "\0b";
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(item, value);
            }
            else if (property.PropertyType == typeof(string[]) || property.PropertyType == typeof(IReadOnlyList<string>))
            {
                property.SetValue(item, new[] { value });
            }
        }

        item.SetProviderId("Imdb\0", "tt\0");
        item.ImageInfos = [new ItemImageInfo { Path = "/images/a\0.jpg", Type = ImageType.Primary }];
        return item;
    }

    private void SaveSeriesWithChildren(Series series, Season season, Episode episode)
    {
        // A refresh sets the series key, then copies it onto the children it saves next.
        _service.SaveItems([series], CancellationToken.None);
        season.SeriesPresentationUniqueKey = series.PresentationUniqueKey;
        episode.SeriesPresentationUniqueKey = series.PresentationUniqueKey;
        _service.SaveItems([season, episode], CancellationToken.None);
    }

    private IReadOnlyList<Guid> FindIds(BaseItemKind kind, string seriesKey)
    {
        return _repository.GetItemList(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [kind],
            SeriesPresentationUniqueKey = seriesKey
        }).Select(e => e.Id).ToArray();
    }
}
