using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A library written before NUL characters were removed can hold them in the keys that tie a series to its
/// seasons and episodes. The rows are seeded as that code stored them, straight into the database, the
/// upgrade routine runs, and the series is then loaded and saved the way the application does.
/// </summary>
public sealed class ItemPersistenceLegacyNulTests : JellyfinDbTestFixture
{
    private const string TvdbId = "12\03";

    private readonly ItemPersistenceService _service;
    private readonly BaseItemRepository _repository;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly User _user = new("test", "auth", "reset");
    private readonly Guid _seriesId = Guid.NewGuid();
    private readonly Guid _seasonId = Guid.NewGuid();
    private readonly Guid _episodeId = Guid.NewGuid();

    public ItemPersistenceLegacyNulTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        _libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = _libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            appHost.Object,
            NullLogger<ItemPersistenceService>.Instance);
        _repository = new BaseItemRepository(
            CreateDbContextFactory(),
            appHost.Object,
            new ItemTypeLookup(),
            configurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);

        _libraryManager.Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns((InternalItemsQuery query) => _repository.GetItemList(query));
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LegacyLibrary_LoadedAndNotSaved_SeasonsAndEpisodesAreFoundThroughTheSeries(bool automaticGrouping)
    {
        SeedLegacyLibrary(automaticGrouping);
        await RunRoutineAsync();

        var series = LoadSeries();

        AssertChildrenFound(series);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LegacyLibrary_OnlyTheSeriesSaved_SeasonsAndEpisodesAreFoundThroughTheSeries(bool automaticGrouping)
    {
        SeedLegacyLibrary(automaticGrouping);
        await RunRoutineAsync();
        var series = LoadSeries();

        _service.SaveItems([series], CancellationToken.None);

        AssertChildrenFound(series);
    }

    [Fact]
    public async Task LegacyLibrary_SeriesSavedAfterTheKeyIsRecomputed_SeasonsAndEpisodesAreFoundThroughTheSeries()
    {
        SeedLegacyLibrary(automaticGrouping: true);
        await RunRoutineAsync();
        var series = LoadSeries();

        series.PresentationUniqueKey = series.CreatePresentationUniqueKey();
        _service.SaveItems([series], CancellationToken.None);
        CopySeriesKeyOntoChildren(series);

        AssertChildrenFound(series);
    }

    [Fact]
    public async Task LegacyDetachedUserData_KeyedWithNul_IsReattachedToTheItemAfterTheRoutine()
    {
        var userId = Guid.NewGuid();
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Black Widow" };
        movie.SetProviderId(MetadataProvider.Imdb, "tt0001");
        using (var context = CreateDbContext())
        {
            context.Users.Add(new User("viewer", "auth", "reset") { Id = userId });
            context.BaseItems.Add(new BaseItemEntity { Id = movie.Id, Type = typeof(Movie).FullName! });
            context.UserData.Add(new UserData
            {
                ItemId = BaseItemRepository.PlaceholderId,
                Item = null,
                UserId = userId,
                User = null,
                CustomDataKey = "placeholder",
                PlayCount = 3,
                Played = true,
                RetentionDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync(CancellationToken.None);

            // Detached before NUL characters were removed, so it is stored under the key as it was reported.
            context.UserData
                .Where(e => e.CustomDataKey == "placeholder")
                .ExecuteUpdate(s => s.SetProperty(e => e.CustomDataKey, "tt0001\0"));
        }

        await RunRoutineAsync();
        await _service.ReattachUserDataAsync(movie, CancellationToken.None);

        using (var context = CreateDbContext())
        {
            var rows = context.UserData.AsNoTracking().Where(e => e.UserId.Equals(userId)).ToList();
            Assert.DoesNotContain(rows, e => e.ItemId.Equals(BaseItemRepository.PlaceholderId));
            Assert.All(rows, row =>
            {
                Assert.Equal(movie.Id, row.ItemId);
                Assert.Equal(3, row.PlayCount);
                Assert.True(row.Played);
            });
            Assert.Contains(rows, e => e.CustomDataKey == "tt0001");
        }
    }

    private static bool HasNul(string? value) => value?.Contains('\0', StringComparison.Ordinal) == true;

    private Task RunRoutineAsync() => new RemoveNulFromItemKeys(
        new StartupLogger<RemoveNulFromItemKeys>(NullLogger<RemoveNulFromItemKeys>.Instance),
        CreateDbContextFactory()).PerformAsync(CancellationToken.None);

    // Writes the rows as code that did not remove NUL stored them: the series, its season and its episode
    // carry the key built from a provider id with a NUL in it. Returns the series key.
    private string SeedLegacyLibrary(bool automaticGrouping)
    {
        var series = new Series { Id = _seriesId, Name = "Breaking Bad" };
        series.SetProviderId(MetadataProvider.Tvdb, "123");
        var season = new Season { Id = _seasonId, Name = "Season 1", IndexNumber = 1, SeriesId = _seriesId };
        var episode = new Episode { Id = _episodeId, Name = "Pilot", SeriesId = _seriesId, SeasonId = _seasonId };
        _service.SaveItems([series, season, episode], CancellationToken.None);

        // The key is the one the grouped form of the library produced, whatever the option is now.
        _libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { EnableAutomaticSeriesGrouping = true });
        var legacySeries = new Series { Id = _seriesId, Name = "Breaking Bad" };
        legacySeries.ProviderIds[nameof(MetadataProvider.Tvdb)] = TvdbId;
        var legacyKey = legacySeries.CreatePresentationUniqueKey();

        _libraryManager.Setup(l => l.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions { EnableAutomaticSeriesGrouping = automaticGrouping });

        using var context = CreateDbContext();
        context.BaseItemProviders
            .Where(e => e.ItemId.Equals(_seriesId))
            .ExecuteUpdate(s => s.SetProperty(e => e.ProviderValue, TvdbId));
        context.BaseItems
            .Where(e => e.Id.Equals(_seriesId))
            .ExecuteUpdate(s => s.SetProperty(e => e.PresentationUniqueKey, legacyKey));
        context.BaseItems
            .Where(e => e.Id.Equals(_seasonId))
            .ExecuteUpdate(s => s
                .SetProperty(e => e.PresentationUniqueKey, legacyKey + "-001")
                .SetProperty(e => e.SeriesPresentationUniqueKey, legacyKey));
        context.BaseItems
            .Where(e => e.Id.Equals(_episodeId))
            .ExecuteUpdate(s => s.SetProperty(e => e.SeriesPresentationUniqueKey, legacyKey));

        Assert.True(HasNul(legacyKey));
        Assert.True(HasNul(context.BaseItems.Single(e => e.Id.Equals(_seriesId)).PresentationUniqueKey));
        Assert.True(HasNul(context.BaseItems.Single(e => e.Id.Equals(_episodeId)).SeriesPresentationUniqueKey));
        Assert.True(HasNul(context.BaseItemProviders.Single(e => e.ItemId.Equals(_seriesId)).ProviderValue));
        return legacyKey;
    }

    private Series LoadSeries()
    {
        return Assert.IsType<Series>(Assert.Single(_repository.GetItemList(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Series]
        })));
    }

    // What a refresh with automatic series grouping does after it has saved the series. It is the part of
    // SeriesMetadataService.UpdateSeriesChildrenInfoAsync that matters here: children whose
    // SeriesPresentationUniqueKey differs from the series key get the series key and are saved. It leaves out
    // reloading the children of the series, linking episodes to seasons through SeasonId, the update type and
    // the item update events that UpdateToRepositoryAsync raises, and the removal and creation of seasons that
    // follows.
    private void CopySeriesKeyOntoChildren(Series series)
    {
        var seriesKey = series.GetPresentationUniqueKey();
        var children = _repository.GetItemList(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Season, BaseItemKind.Episode],
            ItemIds = [_seasonId, _episodeId]
        });
        foreach (var child in children.Where(c => !string.Equals(((IHasSeries)c).SeriesPresentationUniqueKey, seriesKey, StringComparison.Ordinal)))
        {
            ((IHasSeries)child).SeriesPresentationUniqueKey = seriesKey;
            _service.SaveItems([child], CancellationToken.None);
        }
    }

    private void AssertChildrenFound(Series series)
    {
        Assert.Equal([_seasonId], series.GetSeasons(_user, new DtoOptions(true)).Select(e => e.Id));

        var episodes = _repository.GetItemList(new InternalItemsQuery(_user)
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            SeriesPresentationUniqueKey = series.GetPresentationUniqueKey()
        });
        Assert.Equal([_episodeId], episodes.Select(e => e.Id));
    }
}
