using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the ItemValues get-or-create in <c>ItemPersistenceService.UpdateOrInsertItems</c>.
/// ItemValues de-duplicates on a unique (Type, Value) index, and the scanner saves items in
/// parallel, so two items introducing the same new value race to create it. These tests run on a
/// file-backed SQLite database rather than the shared in-memory fixture, because the race needs a
/// second connection that can commit while the save under test is still open.
/// </summary>
public sealed class ItemPersistenceServiceItemValueRaceTests : IDisposable
{
    private readonly string _databaseFile;
    private readonly string _connectionString;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths = new Mock<IApplicationPaths>().Object;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private CompetingItemValueWriter? _competingWriter;

    public ItemPersistenceServiceItemValueRaceTests()
    {
        _databaseFile = Path.Combine(Path.GetTempPath(), $"jf-itemvalue-race-{Guid.NewGuid():N}.db");
        _connectionString = new SqliteConnectionStringBuilder { DataSource = _databaseFile }.ToString();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connectionString)
            .AddInterceptors(new CompetingWriterInterceptor(() => _competingWriter))
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
        context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");

        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;

        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _databaseFile, _databaseFile + "-wal", _databaseFile + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ItemValues_UniqueTypeValueIndex_IsEnforced()
    {
        // The premise of the race: the schema really does reject a duplicate (Type, Value), so a
        // plain insert by the losing writer aborts its transaction rather than being ignored.
        using var context = CreateDbContext();
        InsertItemValue(context, Guid.NewGuid(), ItemValueType.Genre, "Drama");

        Assert.ThrowsAny<DbException>(() => InsertItemValue(context, Guid.NewGuid(), ItemValueType.Genre, "Drama"));
    }

    [Fact]
    public void SaveItems_TwoItemsSharingANewValue_CreateItOnce()
    {
        var first = CreateItem("Drama");
        var second = CreateItem("Drama");

        CreateService().SaveItems([first, second], CancellationToken.None);

        Assert.Single(StoredValues("Drama"));
        Assert.Equal(2, MappedItemCount("Drama"));
    }

    [Fact]
    public void SaveItems_ValueAlreadyExists_ReusesItInsteadOfInserting()
    {
        // The state a retry converges to: once the winner's row is committed, the loser's next
        // attempt must map onto it rather than try to create its own.
        var service = CreateService();
        service.SaveItems([CreateItem("Drama")], CancellationToken.None);
        service.SaveItems([CreateItem("Drama")], CancellationToken.None);

        Assert.Single(StoredValues("Drama"));
        Assert.Equal(2, MappedItemCount("Drama"));
    }

    [Fact]
    public void SaveItems_LosesRaceToCreateTheValue_RetriesOntoTheWinnersRow()
    {
        // Reproduces the reported failure. SQLite serialises writers, which is why the bug never
        // shows up there and why a second thread cannot stage it: the save under test already holds
        // the write lock by the time it inserts. The race is instead played out through the seams
        // the service is constructed with, and both halves that matter stay real. The interceptor
        // writes the contested value into the failing save's own transaction, so the exception comes
        // from the actual unique index rather than a fabricated one, and the winning row is then
        // committed for real, from its own connection, before the service re-reads.
        _competingWriter = new CompetingItemValueWriter(_connectionString, ItemValueType.Genre, "Drama");

        CreateService().SaveItems([CreateItem("Drama")], CancellationToken.None);

        Assert.True(_competingWriter.ProvokedConflict, "the save never hit the unique index, so no race was staged");
        Assert.True(_competingWriter.Committed, "the winning row was never committed, so there was nothing to retry onto");

        // One row survives, it is the winner's, and the item is mapped onto it: the losing save must
        // not keep the ItemValueId it generated before the race.
        var stored = StoredValues("Drama");
        Assert.Single(stored);
        Assert.Equal(_competingWriter.ItemValueId, stored[0].ItemValueId);
        Assert.Equal(1, MappedItemCount("Drama"));
    }

    private static BaseItem CreateItem(string genre)
        => new Book
        {
            Id = Guid.NewGuid(),
            Name = "Book",
            Genres = [genre]
        };

    private static void InsertItemValue(JellyfinDbContext context, Guid id, ItemValueType type, string value)
        => context.Database.ExecuteSql(
            $"""INSERT INTO "ItemValues" ("ItemValueId", "Type", "Value", "CleanValue") VALUES ({id}, {(int)type}, {value}, {value})""");

    private List<ItemValue> StoredValues(string value)
    {
        using var context = CreateDbContext();
        return context.ItemValues.Where(e => e.Type == ItemValueType.Genre && e.Value == value).ToList();
    }

    private int MappedItemCount(string value)
    {
        using var context = CreateDbContext();
        return context.ItemValuesMap.Count(e => e.ItemValue.Type == ItemValueType.Genre && e.ItemValue.Value == value);
    }

    private ItemPersistenceService CreateService()
        => new(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
    {
        // The first context the service asks for after its save failed is the one it checks the
        // race with, and by then its transaction is gone and the write lock is free. That is the
        // point the winning row can actually be committed.
        JellyfinDbContext CreateDbContextForService()
        {
            _competingWriter?.CommitOnce();
            return CreateDbContext();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContextForService);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContextForService);

        return factory.Object;
    }

    /// <summary>
    /// Stages the loss of a race for one (Type, Value), in the two steps it happens in: provoke the
    /// unique-index violation inside the save that is about to fail, then commit the winning row
    /// from a separate connection once that save has rolled back and released the write lock.
    /// </summary>
    private sealed class CompetingItemValueWriter
    {
        private readonly string _connectionString;
        private readonly ItemValueType _type;
        private readonly string _value;

        public CompetingItemValueWriter(string connectionString, ItemValueType type, string value)
        {
            _connectionString = connectionString;
            _type = type;
            _value = value;
            ItemValueId = Guid.NewGuid();
        }

        public Guid ItemValueId { get; }

        public bool ProvokedConflict { get; private set; }

        public bool Committed { get; private set; }

        /// <summary>
        /// Writes the contested value into the save's own transaction, so the insert it is about to
        /// flush collides with the real index. Rolled back with that transaction, which is why the
        /// row still has to be committed afterwards.
        /// </summary>
        /// <param name="context">The context whose save is about to run.</param>
        public void ProvokeConflictOnce(DbContext context)
        {
            if (ProvokedConflict)
            {
                return;
            }

            ProvokedConflict = true;
            context.Database.ExecuteSql(
                $"""INSERT INTO "ItemValues" ("ItemValueId", "Type", "Value", "CleanValue") VALUES ({ItemValueId}, {(int)_type}, {_value}, {_value})""");
        }

        /// <summary>
        /// Commits the winning row from its own connection, as the writer that won the race would
        /// have. Only meaningful once the losing save has rolled back.
        /// </summary>
        public void CommitOnce()
        {
            if (Committed || !ProvokedConflict)
            {
                return;
            }

            Committed = true;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """INSERT INTO "ItemValues" ("ItemValueId", "Type", "Value", "CleanValue") VALUES ($id, $type, $value, $value)""";
            command.Parameters.AddWithValue("$id", ItemValueId);
            command.Parameters.AddWithValue("$type", (int)_type);
            command.Parameters.AddWithValue("$value", _value);
            command.ExecuteNonQuery();
        }
    }

    private sealed class CompetingWriterInterceptor : SaveChangesInterceptor
    {
        private readonly Func<CompetingItemValueWriter?> _writer;

        public CompetingWriterInterceptor(Func<CompetingItemValueWriter?> writer)
        {
            _writer = writer;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
            {
                _writer()?.ProvokeConflictOnce(eventData.Context);
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
