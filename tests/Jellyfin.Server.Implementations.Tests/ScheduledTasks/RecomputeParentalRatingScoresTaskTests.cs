using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.ScheduledTasks;

[Collection(BaseItemStaticsCollection.Name)]
public sealed class RecomputeParentalRatingScoresTaskTests : SqliteDbTestFixture, IAsyncLifetime
{
    private const string Untouched = "untouched";

    private readonly ServerConfiguration _serverConfiguration = new() { MetadataCountryCode = "US" };
    private readonly Mock<IServerConfigurationManager> _configurationManager = new();
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Dictionary<Guid, BaseItem> _cache = [];
    private readonly List<int> _loadChunkSizes = [];
    private readonly List<Guid> _ids = [];
    private readonly Dictionary<Guid, BaseItem> _database = [];

    private LocalizationManager _localization = null!;
    private RatingsStateData _storedState = new();
    private int _stateSaveCount;
    private Action? _onItemsLoaded;

    private ILibraryManager? _previousLibraryManager;
    private IServerConfigurationManager? _previousConfigurationManager;
    private MediaBrowser.Model.Globalization.ILocalizationManager? _previousLocalizationManager;

    public async ValueTask InitializeAsync()
    {
        _configurationManager.SetupGet(x => x.Configuration).Returns(_serverConfiguration);
        _configurationManager
            .Setup(x => x.GetConfiguration(RatingsStateConfigurationFactory.Key))
            .Returns(() => _storedState);
        _configurationManager
            .Setup(x => x.SaveConfiguration(RatingsStateConfigurationFactory.Key, It.IsAny<object>()))
            .Callback<string, object>((_, data) =>
            {
                _storedState = (RatingsStateData)data;
                _stateSaveCount++;
            });

        _localization = new LocalizationManager(_configurationManager.Object, NullLogger<LocalizationManager>.Instance);
        await _localization.LoadAll();

        _libraryManager.Setup(x => x.GetVirtualFolders()).Returns([]);

        // Only a query for every item, owned items included, returns the ids.
        _libraryManager
            .Setup(x => x.GetItemIds(It.Is<InternalItemsQuery>(q => q.Recursive && q.IncludeOwnedItems && q.IsVirtualItem == null && q.User == null)))
            .Returns(() => _ids.ToArray());
        _libraryManager
            .Setup(x => x.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeOwnedItems && q.ItemIds.Length > 0)))
            .Returns((InternalItemsQuery query) =>
            {
                _loadChunkSizes.Add(query.ItemIds.Length);
                var items = query.ItemIds.Where(_database.ContainsKey).Select(id => _database[id]).ToArray();
                _onItemsLoaded?.Invoke();
                return items;
            });
        _libraryManager.Setup(x => x.GetItemById(It.IsAny<Guid>())).Returns((Guid id) => _cache.GetValueOrDefault(id));
        _libraryManager.Setup(x => x.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        _libraryManager.Setup(x => x.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(() => new LibraryOptions());

        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousLocalizationManager = BaseItem.LocalizationManager;
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.ConfigurationManager = _configurationManager.Object;
        BaseItem.LocalizationManager = _localization;
    }

    public ValueTask DisposeAsync()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.ConfigurationManager = _previousConfigurationManager;
        BaseItem.LocalizationManager = _previousLocalizationManager;
        return ValueTask.CompletedTask;
    }

    private RatingsStateStore CreateStateStore()
        => new(_configurationManager.Object, _libraryManager.Object, () => "v1");

    private RecomputeParentalRatingScoresTask CreateTask()
        => new(
            _libraryManager.Object,
            CreateDbContextFactory(),
            CreateStateStore(),
            _localization,
            NullLogger<RecomputeParentalRatingScoresTask>.Instance);

    /// <summary>
    /// Adds an item to the library and a row for it to the database. The row holds the item's stored scores unless
    /// other scores are given, which lets a test tell "left alone" from "rewritten with the same value".
    /// </summary>
    private T Add<T>(T item, bool inCache = true, int? rowScore = null, int? rowSubScore = null, bool rowDiffers = false)
        where T : BaseItem
    {
        item.Id = item.Id.Equals(Guid.Empty) ? Guid.NewGuid() : item.Id;
        _database[item.Id] = item;
        _ids.Add(item.Id);
        if (inCache)
        {
            _cache[item.Id] = item;
        }

        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = item.Id,
            Type = item.GetType().FullName!,
            Name = Untouched,
            OfficialRating = Untouched,
            Data = Untouched,
            InheritedParentalRatingValue = rowDiffers ? rowScore : item.InheritedParentalRatingValue,
            InheritedParentalRatingSubValue = rowDiffers ? rowSubScore : item.InheritedParentalRatingSubValue
        });
        context.SaveChanges();

        return item;
    }

    private (int? Score, int? SubScore) Row(BaseItem item)
    {
        using var context = CreateDbContext();
        var row = context.BaseItems.Single(e => e.Id.Equals(item.Id));
        return (row.InheritedParentalRatingValue, row.InheritedParentalRatingSubValue);
    }

    private Task Run(CancellationToken cancellationToken = default)
        => CreateTask().ExecuteAsync(new Progress<double>(), cancellationToken);

    [Fact]
    public void Task_Metadata_HasLibraryCategoryAndNoDefaultTriggers()
    {
        var task = CreateTask();

        Assert.Equal("RecomputeParentalRatingScores", task.Key);
        Assert.Empty(task.GetDefaultTriggers());
        Assert.Equal("Library", task.Category);
        Assert.Equal("Recompute Parental Rating Scores", task.Name);
    }

    [Fact]
    public async Task Execute_StaleScore_IsReplacedByComputedScore()
    {
        var movie = Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99, InheritedParentalRatingSubValue = 7 });
        var expected = _localization.GetRatingScore("PG-13", "US");
        Assert.NotNull(expected);

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal((expected.Score, expected.SubScore), Row(movie));
        Assert.Equal(expected.Score, movie.InheritedParentalRatingValue);
    }

    [Fact]
    public async Task Execute_UpToDateItem_IsNotWritten()
    {
        var expected = _localization.GetRatingScore("PG-13", "US")!;

        // The row differs from the item on purpose: an item that is up to date must not be written at all.
        var movie = Add(
            new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = expected.Score, InheritedParentalRatingSubValue = expected.SubScore },
            rowScore: 5,
            rowSubScore: 5,
            rowDiffers: true);

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal((5, 5), Row(movie));
    }

    [Fact]
    public async Task Execute_ChangedServerCountry_UpdatesScore()
    {
        // The rating string stays the same, only the table it is looked up in changes.
        const string Rating = "PG";
        var us = _localization.GetRatingScore(Rating, "US");
        var gb = _localization.GetRatingScore(Rating, "GB");
        Assert.NotNull(us);
        Assert.NotNull(gb);
        Assert.NotEqual(us.Score, gb.Score);

        var movie = Add(new Movie { OfficialRating = Rating, InheritedParentalRatingValue = us.Score, InheritedParentalRatingSubValue = us.SubScore });
        await Run(TestContext.Current.CancellationToken);
        Assert.Equal((us.Score, us.SubScore), Row(movie));

        _serverConfiguration.MetadataCountryCode = "GB";
        await Run(TestContext.Current.CancellationToken);

        Assert.Equal((gb.Score, gb.SubScore), Row(movie));
    }

    [Fact]
    public async Task Execute_ItemCountry_IsRespected()
    {
        var movie = Add(new Movie { OfficialRating = "PG", PreferredMetadataCountryCode = "GB", InheritedParentalRatingValue = 99 });
        var expected = _localization.GetRatingScore("PG", "GB");
        Assert.NotEqual(_localization.GetRatingScore("PG", "US")!.Score, expected!.Score);

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal(expected.Score, Row(movie).Score);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_EpisodeWithoutRating_InheritsFromParentRatingNotFromParentStoredScore(bool childrenFirst)
    {
        // The stored scores of the parents are stale (99) and the children are processed before or after them:
        // the result of a child must only depend on the rating strings of its parents.
        var series = new Series { Id = Guid.NewGuid(), OfficialRating = "TV-MA", InheritedParentalRatingValue = 99 };
        var season = new Season { Id = Guid.NewGuid(), ParentId = series.Id, SeriesId = series.Id, InheritedParentalRatingValue = 99 };
        var episode = new Episode { Id = Guid.NewGuid(), ParentId = season.Id, SeasonId = season.Id, InheritedParentalRatingValue = 99 };
        BaseItem[] parts = [series, season, episode];
        foreach (var part in childrenFirst ? parts.Reverse() : parts)
        {
            Add(part);
        }

        await Run(TestContext.Current.CancellationToken);

        var expected = _localization.GetRatingScore("TV-MA", "US");
        Assert.NotNull(expected);
        foreach (var part in parts)
        {
            Assert.Equal((expected.Score, expected.SubScore), Row(part));
        }
    }

    [Fact]
    public async Task Execute_CustomRating_WinsOverOfficialRating()
    {
        var movie = Add(new Movie { OfficialRating = "R", CustomRating = "G", InheritedParentalRatingValue = 99 });
        var expected = _localization.GetRatingScore("G", "US")!;

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal((expected.Score, expected.SubScore), Row(movie));
    }

    [Fact]
    public async Task Execute_ItemThatBecameUnrated_ClearsBothValues()
    {
        var movie = Add(new Movie { OfficialRating = null, InheritedParentalRatingValue = 17, InheritedParentalRatingSubValue = 2 });

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal(((int?)null, (int?)null), Row(movie));
        Assert.Null(movie.InheritedParentalRatingValue);
        Assert.Null(movie.InheritedParentalRatingSubValue);
    }

    [Fact]
    public async Task Execute_ChangesOnlyTheTwoScoreColumns_AndNeverUsesTheMetadataSavers()
    {
        var movie = Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 });

        await Run(TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        var row = context.BaseItems.Single(e => e.Id.Equals(movie.Id));
        Assert.Equal((Untouched, Untouched, Untouched), (row.Name, row.OfficialRating, row.Data));
        _libraryManager.Verify(
            x => x.UpdateItemsAsync(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateScores_WritesOnlyTheGivenRows()
    {
        var a = Add(new Movie { InheritedParentalRatingValue = 1, InheritedParentalRatingSubValue = 1 });
        var b = Add(new Movie { InheritedParentalRatingValue = 2, InheritedParentalRatingSubValue = 2 });
        var c = Add(new Movie { InheritedParentalRatingValue = 3, InheritedParentalRatingSubValue = 3 });

        await CreateTask().UpdateScoresAsync([(a.Id, 10, 1), (b.Id, null, null)], TestContext.Current.CancellationToken);

        Assert.Equal((10, 1), Row(a));
        Assert.Equal(((int?)null, (int?)null), Row(b));
        Assert.Equal((3, 3), Row(c));
        using var context = CreateDbContext();
        Assert.All(context.BaseItems.Where(row => row.Name == Untouched), row => Assert.Equal((Untouched, Untouched, Untouched), (row.Name, row.OfficialRating, row.Data)));
    }

    [Fact]
    public async Task Execute_ManyItems_AreLoadedInBoundedChunks()
    {
        const int Count = (RecomputeParentalRatingScoresTask.ChunkSize * 2) + 200;
        var movies = new List<Movie>();
        for (var i = 0; i < Count; i++)
        {
            movies.Add(Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 }, inCache: false));
        }

        var reported = new List<double>();
        await CreateTask().ExecuteAsync(new SynchronousProgress(reported.Add), TestContext.Current.CancellationToken);

        Assert.Equal([RecomputeParentalRatingScoresTask.ChunkSize, RecomputeParentalRatingScoresTask.ChunkSize, 200], _loadChunkSizes);
        using var context = CreateDbContext();
        Assert.DoesNotContain(context.BaseItems, row => row.InheritedParentalRatingValue == 99);
        Assert.Equal(100d, reported[^1]);
        Assert.Equal(reported.Order(), reported);
    }

    [Fact]
    public async Task Execute_ItemDeletedAfterIdsWereRead_IsIgnored()
    {
        var movie = Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 });
        _ids.Add(Guid.NewGuid());

        await Run(TestContext.Current.CancellationToken);

        Assert.NotEqual(99, Row(movie).Score);
    }

    [Fact]
    public async Task Execute_NoItems_CompletesReports100AndStoresState()
    {
        var reported = new List<double>();

        await CreateTask().ExecuteAsync(new SynchronousProgress(reported.Add), TestContext.Current.CancellationToken);

        Assert.Equal(100d, reported[^1]);
        Assert.Equal(1, _stateSaveCount);
        Assert.NotEmpty(_storedState.RatingsDataVersion);
    }

    [Fact]
    public async Task Execute_Success_StoresTheStateTakenAtTheStartOfTheRun()
    {
        var store = CreateStateStore();
        var startState = store.ComputeCurrent();
        Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 });

        // The server country changes while the run is loading its first chunk.
        _onItemsLoaded = () => _serverConfiguration.MetadataCountryCode = "DE";

        await Run(TestContext.Current.CancellationToken);

        Assert.Equal(1, _stateSaveCount);
        Assert.Equal(startState, store.GetStored());
        Assert.True(store.IsRecomputeNeeded());
    }

    [Fact]
    public async Task Execute_NothingChangedDuringRun_LeavesNothingToRecompute()
    {
        Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 });

        await Run(TestContext.Current.CancellationToken);

        Assert.False(CreateStateStore().IsRecomputeNeeded());
    }

    [Fact]
    public async Task Execute_Cancelled_StopsBetweenChunksAndDoesNotStoreState()
    {
        using var cts = new CancellationTokenSource();
        for (var i = 0; i < RecomputeParentalRatingScoresTask.ChunkSize * 2; i++)
        {
            Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 }, inCache: false);
        }

        _onItemsLoaded = cts.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(cts.Token));

        Assert.Single(_loadChunkSizes);
        Assert.Equal(0, _stateSaveCount);
    }

    [Fact]
    public async Task Execute_ChangedItem_UpdatesTheCachedInstance()
    {
        var fresh = new Series { Id = Guid.NewGuid(), OfficialRating = "TV-MA", InheritedParentalRatingValue = 99 };
        var cached = new Series { Id = fresh.Id, OfficialRating = "TV-MA", InheritedParentalRatingValue = 99 };
        Add(fresh, inCache: false);
        var task = CreateTask();
        task.CachedItemLookup = id => id.Equals(fresh.Id) ? cached : null;

        await task.ExecuteAsync(new Progress<double>(), TestContext.Current.CancellationToken);

        Assert.Equal(fresh.InheritedParentalRatingValue, cached.InheritedParentalRatingValue);
        Assert.NotEqual(99, cached.InheritedParentalRatingValue);
    }

    [Fact]
    public async Task Execute_ChangedItemThatIsNotCached_IsNotLoadedByIdFromTheDatabase()
    {
        Add(new Movie { OfficialRating = "PG-13", InheritedParentalRatingValue = 99 }, inCache: false);
        _libraryManager.Invocations.Clear();

        await Run(TestContext.Current.CancellationToken);

        // GetItemById is only called while walking parents, and an uncached movie has none here.
        _libraryManager.Verify(x => x.GetItemById(It.IsAny<Guid>()), Times.Never);
    }

    private sealed class SynchronousProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
