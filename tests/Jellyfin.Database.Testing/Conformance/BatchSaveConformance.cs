using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: the server saves a batch of new items, such as the new children a scan finds in a folder, in which the
/// engine refuses the row of one item. <see cref="ItemPersistenceService.SaveItems"/> saves all of the items or none, so
/// that one row costs the whole batch; <see cref="ItemPersistenceService.SaveItemsSkippingFailures"/> leaves out only
/// that item. Every engine refuses an item whose parent does not exist, by its foreign key. A path longer than an
/// index key may be is saved on every engine but PostgreSQL, which indexes the whole value: that is a known gap, so the
/// row fails once PostgreSQL saves such a path, and the gap has to be removed.
/// </summary>
public abstract class BatchSaveConformance : ConformanceTestBase
{
    private const int BatchSize = 10;
    private const int Refused = 4;

    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    protected BatchSaveConformance()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
    }

    [ConformanceFact]
    public void SaveItems_OneItemWhoseParentIsMissing_SavesNone()
    {
        var items = CreateMovies();
        items[Refused].ParentId = Guid.NewGuid();

        Assert.Throws<DbUpdateException>(() => CreateService().SaveItems(items, CancellationToken.None));

        Assert.Empty(StoredIds(items));
    }

    [ConformanceFact]
    public void SaveItemsSkippingFailures_OneItemWhoseParentIsMissing_SavesTheOthers()
    {
        var items = CreateMovies();
        items[Refused].ParentId = Guid.NewGuid();

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { items[Refused].Id }, notSaved);
        Assert.Equal(items.Where((_, i) => i != Refused).Select(i => i.Id).Order(), StoredIds(items).Order());
    }

    [ConformanceFact]
    public void SaveItemsSkippingFailures_OneItemWithAPathOverTheIndexKeyLimit_SavesEveryItem()
    {
        // Letters in no order, so that the engine cannot compress the key under its limit.
        var random = new Random(7);
        var items = CreateMovies();
        var longPath = items[Refused];
        longPath.Path = "/movies/" + new string(Enumerable.Range(0, 3000).Select(_ => (char)random.Next('a', 'z' + 1)).ToArray());
        var logger = new FailureLogger();

        var notSaved = CreateService(logger).SaveItemsSkippingFailures(items, CancellationToken.None);

        // The other items are saved on every engine, also on one that refuses the long path.
        var stored = StoredIds(items);
        Assert.Equal(items.Where(i => !i.Id.Equals(longPath.Id)).Select(i => i.Id).Order(), stored.Where(id => !id.Equals(longPath.Id)).Order());

        // The item with the long path is saved too, except where the engine refuses it. The error it was left out
        // for is thrown again, so that the gap is the only failure it is taken for.
        KnownGap.On(KnownGaps.PathIndexKeyOnPostgres, () =>
        {
            if (logger.Failures.Count > 0)
            {
                ExceptionDispatchInfo.Throw(Assert.Single(logger.Failures));
            }

            Assert.Empty(notSaved);
            Assert.Contains(longPath.Id, stored);
        });
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    private static List<BaseItem> CreateMovies()
        => Enumerable.Range(0, BatchSize)
            .Select(i => (BaseItem)new Movie { Id = Guid.NewGuid(), Name = "Movie " + i, Path = "/movies/m" + i + ".mkv" })
            .ToList();

    private List<Guid> StoredIds(IEnumerable<BaseItem> items)
    {
        var ids = items.Select(i => i.Id).ToList();
        using var context = CreateDbContext();
        return context.BaseItems.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToList();
    }

    private ItemPersistenceService CreateService(ILogger<ItemPersistenceService>? logger = null)
    {
        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        return new(CreateDbContextFactory(), DatabaseProvider, appHost.Object, logger ?? NullLogger<ItemPersistenceService>.Instance);
    }

    /// <summary>
    /// Keeps the exceptions logged with an error, which are those of the items that were not saved.
    /// </summary>
    private sealed class FailureLogger : ILogger<ItemPersistenceService>
    {
        public List<Exception> Failures { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error && exception is not null)
            {
                Failures.Add(exception);
            }
        }
    }
}
