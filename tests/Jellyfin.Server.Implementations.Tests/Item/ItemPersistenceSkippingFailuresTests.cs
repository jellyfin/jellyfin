using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A batch of items is saved in one transaction, so a row that the database refuses fails the whole batch: a folder
/// whose scan finds one such file saves none of its new items. Saving with
/// <see cref="ItemPersistenceService.SaveItemsSkippingFailures"/> leaves out only the refused item and what belongs to it.
/// The refusal comes from a trigger that SQLite runs for a path under <see cref="RefusedFolder"/>, or from a command
/// interceptor where the test needs an error that no row causes. The trigger and the result codes are SQLite's, so the
/// class runs on SQLite only.
/// </summary>
public sealed class ItemPersistenceSkippingFailuresTests : DatabaseTestFixture
{
    private const string RefusedFolder = "/movies/refused/";
    private const int BatchSize = 10;

    private readonly FailingCommands _failingCommands;
    private readonly AddedItemRecorder _addedItems;
    private readonly RecordingLogger _logger = new();
    private readonly IServerApplicationHost _appHost;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public ItemPersistenceSkippingFailuresTests()
        : this(new FailingCommands(), new AddedItemRecorder())
    {
    }

    private ItemPersistenceSkippingFailuresTests(FailingCommands failingCommands, AddedItemRecorder addedItems)
        : base(TestSchemaSource.Migrations, true, failingCommands, addedItems)
    {
        _failingCommands = failingCommands;
        _addedItems = addedItems;

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

        // Video.SourceType asks this whether the file is an in-progress recording.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        _appHost = appHost.Object;

        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw(
            $"""
            CREATE TRIGGER RefusePath BEFORE INSERT ON BaseItems WHEN NEW.Path LIKE '{RefusedFolder}%'
            BEGIN SELECT RAISE(ABORT, 'path refused by the test'); END
            """);
    }

    // A cancellation thrown as itself is not wrapped in a DbUpdateException, so it never reaches the fallback; the
    // "cancelled" case is one that a client wraps in an exception of its own.
    public static TheoryData<string> ErrorsNotOfTheData => new()
    {
        "busy",
        "storage",
        "timeout",
        "transport",
        "transient",
        "cancelled",
        "not a save"
    };

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItems_OneItemRefused_SavesNoneAndThrows()
    {
        var items = CreateMovies(refused: [4]);

        var exception = Assert.Throws<DbUpdateException>(() => CreateService().SaveItems(items, CancellationToken.None));

        Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Empty(StoredIds(items));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_OneItemRefused_SavesTheOthersAndLogsTheRefusedOne()
    {
        var items = CreateMovies(refused: [4]);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { items[4].Id }, notSaved);
        Assert.Equal(items.Where((_, i) => i != 4).Select(i => i.Id).Order(), StoredIds(items).Order());
        var error = Assert.Single(_logger.Errors);
        Assert.Contains(items[4].Id.ToString(), error.Message, StringComparison.Ordinal);
        Assert.Contains(items[4].Path, error.Message, StringComparison.Ordinal);
        Assert.IsType<SqliteException>(Assert.IsType<DbUpdateException>(error.Exception).InnerException);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_NothingRefused_SavesTheBatchInOneGo()
    {
        var items = CreateMovies(refused: []);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Empty(notSaved);
        Assert.Equal(BatchSize, StoredIds(items).Count);
        Assert.Equal(new[] { BatchSize }, _addedItems.Saves.Select(s => s.Count));
        Assert.Empty(_logger.Entries);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_FirstItemsRefusedAlike_ThrowsTheErrorOfTheBatchAndSavesNothingMore()
    {
        var items = CreateMovies(refused: [0, 1, 2]);

        var exception = Assert.Throws<DbUpdateException>(() => CreateService().SaveItemsSkippingFailures(items, CancellationToken.None));

        Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Empty(StoredIds(items));

        // The batch, then the three items one at a time, and no more.
        Assert.Equal(new[] { BatchSize, 1, 1, 1 }, _addedItems.Saves.Select(s => s.Count));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_FirstTwoItemsRefused_SavesTheRest()
    {
        var items = CreateMovies(refused: [0, 1]);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { items[0].Id, items[1].Id }.Order(), notSaved.Order());
        Assert.Equal(BatchSize - 2, StoredIds(items).Count);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_FirstItemsFailWithDifferentSqlStates_SavesTheRest()
    {
        // A test of how failures are compared: SQLite reports no SQLSTATE, so the errors are made up, and any
        // failures of SQLite itself count as alike.
        var items = CreateMovies(refused: []);
        _failingCommands.Fail(items[0].Path, () => new TestDbException(isTransient: false, sqlState: "22001"));
        _failingCommands.Fail(items[1].Path, () => new TestDbException(isTransient: false, sqlState: "22001"));
        _failingCommands.Fail(items[2].Path, () => new TestDbException(isTransient: false, sqlState: "23514"));

        var notSaved = CreateService(EveryErrorADataError()).SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { items[0].Id, items[1].Id, items[2].Id }.Order(), notSaved.Order());
        Assert.Equal(BatchSize - 3, StoredIds(items).Count);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_EveryItemRefused_ThrowsTheErrorOfTheBatch()
    {
        var items = CreateMovies(refused: [0, 1]).Take(2).ToList();

        var exception = Assert.Throws<DbUpdateException>(() => CreateService().SaveItemsSkippingFailures(items, CancellationToken.None));

        Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Empty(StoredIds(items));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_SingleItemRefused_Throws()
    {
        var items = CreateMovies(refused: [0]).Take(1).ToList();

        Assert.Throws<DbUpdateException>(() => CreateService().SaveItemsSkippingFailures(items, CancellationToken.None));

        Assert.Equal(new[] { 1 }, _addedItems.Saves.Select(s => s.Count));
    }

    [ProviderTheory(TestDbProvider.Sqlite)]
    [MemberData(nameof(ErrorsNotOfTheData))]
    public void SaveItemsSkippingFailures_ErrorNotOfTheData_IsThrownWithoutSavingItemByItem(string error)
    {
        var items = CreateMovies(refused: []);

        // The provider is asked last, so a provider that calls every error a data error shows that the guards before
        // it hold; the errors of SQLite itself are classified by the real provider.
        var provider = DatabaseProvider;
        switch (error)
        {
            case "busy":
                _failingCommands.Fail(items[4].Path, () => new SqliteException("database is locked", 5));
                break;
            case "storage":
                _failingCommands.Fail(items[4].Path, () => new SqliteException("disk I/O error", 10));
                break;
            case "timeout":
                _failingCommands.Fail(items[4].Path, () => new TimeoutException());
                provider = EveryErrorADataError();
                break;
            case "transport":
                _failingCommands.Fail(items[4].Path, () => new TestDbException(isTransient: false, sqlState: null, new IOException("connection reset")));
                provider = EveryErrorADataError();
                break;
            case "transient":
                _failingCommands.Fail(items[4].Path, () => new TestDbException(isTransient: true, sqlState: "40001"));
                provider = EveryErrorADataError();
                break;
            case "cancelled":
                _failingCommands.Fail(items[4].Path, () => new TestDbException(isTransient: false, sqlState: "57014", new OperationCanceledException()));
                provider = EveryErrorADataError();
                break;
            case "not a save":
                // The items that exist already are read before anything is written.
                _failingCommands.FailReads(() => new TestDbException(isTransient: false, sqlState: "22001"));
                provider = EveryErrorADataError();
                break;
        }

        Assert.ThrowsAny<Exception>(() => CreateService(provider).SaveItemsSkippingFailures(items, CancellationToken.None));

        Assert.Empty(StoredIds(items));
        Assert.Equal(1, _failingCommands.Failures);
        Assert.Empty(_logger.Errors);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_WriteConflictTheRetryGaveUpOn_IsThrownWithoutSavingItemByItem()
    {
        // A violated unique key is a write conflict, which the retry policy repeats, and also a data error. Once the
        // retries are spent, it stays the failure of the batch, as before.
        var items = CreateMovies(refused: []);
        _failingCommands.Fail(items[4].Path, () => new SqliteException("UNIQUE constraint failed: BaseItems.Id", 19, 2067));
        Assert.True(DatabaseProvider.IsDataError(new SqliteException("UNIQUE constraint failed: BaseItems.Id", 19, 2067)));

        Assert.Throws<DbUpdateException>(() => CreateService().SaveItemsSkippingFailures(items, CancellationToken.None));

        Assert.Empty(StoredIds(items));
        Assert.Equal(WriteConflictRetry.MaxAttempts, _failingCommands.Failures);
        Assert.Empty(_logger.Errors);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_PrimaryRefused_SkipsItsAlternateVersion()
    {
        var items = CreateMovies(refused: [4]);
        var alternate = CreateAlternateVersion(items[4], "/movies/m4 - 1080p.mkv");
        items.Add(alternate);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { items[4].Id, alternate.Id }.Order(), notSaved.Order());
        Assert.Equal(BatchSize - 1, StoredIds(items).Count);

        // The alternate is not tried at all, so only the primary's own error is logged with an exception.
        Assert.Equal(2, _logger.Errors.Count);
        Assert.Single(_logger.Errors, e => e.Exception is not null);
        Assert.Single(_logger.Errors, e => e.Exception is null
            && e.Message.Contains(alternate.Id.ToString(), StringComparison.Ordinal)
            && e.Message.Contains(items[4].Id.ToString(), StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_FolderRefused_SkipsItsChildInTheSameBatch()
    {
        var items = CreateMovies(refused: []);
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Refused", Path = RefusedFolder + "folder" };
        var child = new Movie { Id = Guid.NewGuid(), Name = "Child", Path = "/movies/folder/child.mkv", ParentId = folder.Id };
        items.Insert(3, folder);
        items.Insert(6, child);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { folder.Id, child.Id }.Order(), notSaved.Order());
        Assert.Equal(BatchSize, StoredIds(items).Count);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_AlternateVersionRefused_SavesItsPrimary()
    {
        var items = CreateMovies(refused: []);
        var alternate = CreateAlternateVersion(items[4], RefusedFolder + "m4 - 1080p.mkv");
        items.Add(alternate);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { alternate.Id }, notSaved);
        Assert.Equal(BatchSize, StoredIds(items).Count);
        using var context = CreateDbContext();
        Assert.Empty(context.LinkedChildren.Where(e => e.ParentId.Equals(items[4].Id)));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_OneOfTwoAlternateVersionsRefused_LinksThePrimaryToTheOther()
    {
        var items = CreateMovies(refused: []);
        var saved = CreateAlternateVersion(items[4], "/movies/m4 - 1080p.mkv");
        var refused = CreateAlternateVersion(items[4], RefusedFolder + "m4 - 720p.mkv");
        ((Video)items[4]).LocalAlternateVersions = [saved.Path, refused.Path];
        items.Add(saved);
        items.Add(refused);

        var notSaved = CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        Assert.Equal(new[] { refused.Id }, notSaved);
        using var context = CreateDbContext();
        var link = Assert.Single(context.LinkedChildren.Where(e => e.ParentId.Equals(items[4].Id)));
        Assert.Equal(saved.Id, link.ChildId);
        Assert.Equal(DbLinkedChildType.LocalAlternateVersion, link.ChildType);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_OtherItemRefused_SavesPrimaryAndAlternateVersionTogetherWithTheirLink()
    {
        var items = CreateMovies(refused: [1]);
        var alternate = CreateAlternateVersion(items[4], "/movies/m4 - 1080p.mkv");
        items.Add(alternate);

        CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        using var context = CreateDbContext();
        var link = Assert.Single(context.LinkedChildren.Where(e => e.ParentId.Equals(items[4].Id)));
        Assert.Equal(alternate.Id, link.ChildId);
        Assert.Equal(DbLinkedChildType.LocalAlternateVersion, link.ChildType);
        Assert.Equal(items[4].Id, context.BaseItems.Single(e => e.Id.Equals(alternate.Id)).PrimaryVersionId);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void SaveItemsSkippingFailures_OneItemRefused_SavesTheItemsInTheOrderOfTheBatch()
    {
        var items = CreateMovies(refused: [6]);
        var alternate = CreateAlternateVersion(items[2], "/movies/m2 - 1080p.mkv");
        items.Add(alternate);

        CreateService().SaveItemsSkippingFailures(items, CancellationToken.None);

        // After the batch, each item is saved on its own, but the video with its alternate version, which follows the
        // video in the list, are saved together and in the place of the video.
        var saves = _addedItems.Saves.Skip(1).ToList();
        var expected = items.Take(BatchSize).Select(i => i.Id.Equals(items[2].Id) ? new[] { i.Id, alternate.Id }.Order().ToList() : [i.Id]).ToList();
        Assert.Equal(expected, saves.Select(s => s.Order().ToList()));
    }

    private List<BaseItem> CreateMovies(IReadOnlyCollection<int> refused)
        => Enumerable.Range(0, BatchSize)
            .Select(i => (BaseItem)new Movie
            {
                Id = Guid.NewGuid(),
                Name = "Movie " + i,
                Path = (refused.Contains(i) ? RefusedFolder : "/movies/") + "m" + i + ".mkv"
            })
            .ToList();

    private static Video CreateAlternateVersion(BaseItem primary, string path)
    {
        var alternate = new Movie { Id = Guid.NewGuid(), Name = primary.Name, Path = path, OwnerId = primary.Id };
        alternate.SetPrimaryVersionId(primary.Id);
        ((Video)primary).LocalAlternateVersions = [path];
        return alternate;
    }

    private List<Guid> StoredIds(IEnumerable<BaseItem> items)
    {
        var ids = items.Select(i => i.Id).ToList();
        using var context = CreateDbContext();
        return context.BaseItems.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToList();
    }

    private ItemPersistenceService CreateService(IJellyfinDatabaseProvider? provider = null)
        => new(CreateDbContextFactory(), provider ?? DatabaseProvider, _appHost, _logger);

    private static IJellyfinDatabaseProvider EveryErrorADataError()
    {
        var provider = new Mock<IJellyfinDatabaseProvider>();
        provider.Setup(p => p.IsDataError(It.IsAny<Exception>())).Returns(true);
        return provider.Object;
    }

    private sealed class TestDbException(bool isTransient, string? sqlState, Exception? inner = null)
        : DbException("refused by the test", inner)
    {
        public override bool IsTransient => isTransient;

        public override string? SqlState => sqlState;
    }

    /// <summary>
    /// Throws instead of running a command that carries a given value, such as the path of an item, or instead of the
    /// first query.
    /// </summary>
    private sealed class FailingCommands : DbCommandInterceptor
    {
        private readonly Dictionary<string, Func<Exception>> _failures = new(StringComparer.Ordinal);
        private Func<Exception>? _failRead;

        public int Failures { get; private set; }

        public void Fail(string value, Func<Exception> exception) => _failures[value] = exception;

        public void FailReads(Func<Exception> exception) => _failRead = exception;

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            if (_failRead is not null && command.CommandText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                var exception = _failRead();
                _failRead = null;
                Failures++;
                throw exception;
            }

            ThrowIfFailing(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            ThrowIfFailing(command);
            return result;
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            ThrowIfFailing(command);
            return result;
        }

        private void ThrowIfFailing(DbCommand command)
        {
            foreach (DbParameter parameter in command.Parameters)
            {
                if (parameter.Value is string value && _failures.TryGetValue(value, out var exception))
                {
                    Failures++;
                    throw exception();
                }
            }
        }
    }

    /// <summary>
    /// Records the items each save adds.
    /// </summary>
    private sealed class AddedItemRecorder : SaveChangesInterceptor
    {
        public List<List<Guid>> Saves { get; } = [];

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var added = eventData.Context!.ChangeTracker.Entries<BaseItemEntity>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity.Id)
                .ToList();
            if (added.Count > 0)
            {
                Saves.Add(added);
            }

            return result;
        }
    }

    private sealed class RecordingLogger : ILogger<ItemPersistenceService>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public List<(LogLevel Level, string Message, Exception? Exception)> Errors => Entries.Where(e => e.Level == LogLevel.Error).ToList();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Entries.Add((logLevel, formatter(state, exception), exception));
            }
        }
    }
}
