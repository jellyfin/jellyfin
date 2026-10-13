using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Testing;
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
/// Another writer can insert a row between a read and the write that depends on it. SQLite serialises
/// writers, so the race is staged: the other writer's row is inserted inside the failing attempt's own
/// transaction, which makes SQLite itself report the constraint violation, and then committed from a
/// connection of its own before the next attempt reads. The database is a file for that reason.
/// The race is staged on the attempt's own connection, so these tests hold on SQLite, although a transaction there
/// takes the write lock when it begins and a real second writer would be refused at once.
/// </summary>
public sealed class ItemPersistenceWriteConflictTests : IDisposable
{
    private const int ConstraintPrimaryKey = 1555;
    private const int ConstraintUnique = 2067;

    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly string _directory;
    private readonly string _connectionString;
    private readonly SqliteDatabaseProvider _provider = new(
        Mock.Of<IApplicationPaths>(),
        NullLogger<SqliteDatabaseProvider>.Instance);

    public ItemPersistenceWriteConflictTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        _directory = Path.Combine(AppContext.BaseDirectory, "item-write-conflict-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(_directory, "jellyfin.db"),
            Pooling = false
        }.ToString();

        using var context = CreateDbContext(CreateOptions());
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public async Task UserData_ConflictingInsertBetweenReadAndWrite_Retries()
    {
        var userId = Guid.NewGuid();
        var item = new Folder { Id = Guid.NewGuid(), Name = "Marvel" };
        var key = Assert.Single(item.GetUserDataKeys());
        using (var context = CreateDbContext(CreateOptions()))
        {
            context.Users.Add(new User("viewer", "auth", "reset") { Id = userId });
            context.BaseItems.Add(new BaseItemEntity { Id = item.Id, Type = typeof(Folder).FullName! });
            context.UserData.Add(new UserData
            {
                ItemId = BaseItemRepository.PlaceholderId,
                Item = null,
                UserId = userId,
                User = null,
                CustomDataKey = key,
                PlayCount = 3,
                Played = true,
                RetentionDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var race = new InsertRace(
            "DELETE FROM \"UserData\"",
            (connection, transaction) => InsertUserData(connection, transaction, item.Id, userId, key));
        var service = CreateService(race);

        await service.ReattachUserDataAsync(item, CancellationToken.None);

        var failure = Assert.IsType<SqliteException>(Assert.Single(race.Failures));
        Assert.Equal(ConstraintPrimaryKey, failure.SqliteExtendedErrorCode);
        Assert.True(race.WinnerCommitted);

        using var verification = CreateDbContext(CreateOptions());
        var row = Assert.Single(verification.UserData.AsNoTracking().Where(e => e.UserId.Equals(userId)).ToList());
        Assert.Equal(item.Id, row.ItemId);
        Assert.Equal(key, row.CustomDataKey);
        Assert.Equal(3, row.PlayCount);
        Assert.Equal(key, Assert.Single(item.UserData).CustomDataKey);
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void ItemValues_ConflictingInsertBetweenReadAndWrite_Retries()
    {
        var winnerId = Guid.NewGuid();
        var item = new Folder { Id = Guid.NewGuid(), Name = "Marvel", Genres = ["Drama"] };

        var race = new InsertRace(
            "FROM \"ItemValues\"",
            (connection, transaction) => InsertItemValue(connection, transaction, winnerId, ItemValueType.Genre, "Drama"));
        var service = CreateService(race);

        service.SaveItems([item], CancellationToken.None);

        var failure = Assert.IsType<SqliteException>(Assert.Single(race.Failures));
        Assert.Equal(ConstraintUnique, failure.SqliteExtendedErrorCode);
        Assert.True(race.WinnerCommitted);

        using var verification = CreateDbContext(CreateOptions());
        var value = Assert.Single(verification.ItemValues.AsNoTracking().ToList());
        Assert.Equal(winnerId, value.ItemValueId);
        Assert.Equal("Drama", value.Value);
        var map = Assert.Single(verification.ItemValuesMap.AsNoTracking().ToList());
        Assert.Equal(item.Id, map.ItemId);
        Assert.Equal(value.ItemValueId, map.ItemValueId);
    }

    private static void InsertUserData(DbConnection connection, DbTransaction? transaction, Guid itemId, Guid userId, string key)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO \"UserData\" (\"ItemId\", \"UserId\", \"CustomDataKey\", \"PlaybackPositionTicks\", \"PlayCount\", \"IsFavorite\", \"Played\") VALUES ($itemId, $userId, $key, 0, 0, 0, 0)";
        command.Parameters.Add(new SqliteParameter("$itemId", itemId));
        command.Parameters.Add(new SqliteParameter("$userId", userId));
        command.Parameters.Add(new SqliteParameter("$key", key));
        command.ExecuteNonQuery();
    }

    private static void InsertItemValue(DbConnection connection, DbTransaction? transaction, Guid id, ItemValueType type, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO \"ItemValues\" (\"ItemValueId\", \"Type\", \"Value\", \"CleanValue\") VALUES ($id, $type, $value, $value)";
        command.Parameters.Add(new SqliteParameter("$id", id));
        command.Parameters.Add(new SqliteParameter("$type", (int)type));
        command.Parameters.Add(new SqliteParameter("$value", value));
        command.ExecuteNonQuery();
    }

    private DbContextOptions<JellyfinDbContext> CreateOptions(params IInterceptor[] interceptors)
#pragma warning disable RS0030 // Do not use banned APIs: The race needs a database file that a second connection can write to.
        => new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connectionString)
            .AddInterceptors(interceptors)
            .Options;
#pragma warning restore RS0030

    private JellyfinDbContext CreateDbContext(DbContextOptions<JellyfinDbContext> options) => new(
        options,
        NullLogger<JellyfinDbContext>.Instance,
        _provider,
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private ItemPersistenceService CreateService(InsertRace race)
    {
        var options = CreateOptions(race);
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        JellyfinDbContext CreateAfterTheWinnerIsCommitted()
        {
            race.CommitWinnerAfterFailure(_connectionString);
            return CreateDbContext(options);
        }

        factory.Setup(f => f.CreateDbContext()).Returns(CreateAfterTheWinnerIsCommitted);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAfterTheWinnerIsCommitted);

        return new ItemPersistenceService(
            factory.Object,
            _provider,
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);
    }

    // Once a statement containing the marker has run, the next command of that attempt is preceded by the
    // other writer's insert, in the same transaction, so SQLite itself rejects the command. The same insert
    // is committed on a connection of its own once the failed attempt has been rolled back.
    private sealed class InsertRace : DbCommandInterceptor
    {
        private readonly string _marker;
        private readonly Action<DbConnection, DbTransaction?> _insert;
        private bool _armed;
        private bool _staged;

        public InsertRace(string marker, Action<DbConnection, DbTransaction?> insert)
        {
            _marker = marker;
            _insert = insert;
        }

        public List<Exception> Failures { get; } = [];

        public bool WinnerCommitted { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            StageBefore(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            StageBefore(command);
            return ValueTask.FromResult(result);
        }

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            Arm(command);
            return result;
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            Arm(command);
            return ValueTask.FromResult(result);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
            => Failures.Add(eventData.Exception);

        public override Task CommandFailedAsync(
            DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Failures.Add(eventData.Exception);
            return Task.CompletedTask;
        }

        public void CommitWinnerAfterFailure(string connectionString)
        {
            if (Failures.Count == 0 || WinnerCommitted)
            {
                return;
            }

#pragma warning disable RS0030 // Do not use banned APIs: The winner is committed on a connection of its own.
            using var connection = new SqliteConnection(connectionString);
#pragma warning restore RS0030
            connection.Open();
            _insert(connection, null);
            WinnerCommitted = true;
        }

        private void Arm(DbCommand command) => _armed |= command.CommandText.Contains(_marker, StringComparison.Ordinal);

        private void StageBefore(DbCommand command)
        {
            if (_armed && !_staged)
            {
                _staged = true;
                _insert(command.Connection!, command.Transaction);
            }
        }
    }
}
