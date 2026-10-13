using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the file that records which schema migration is running on a provider that cannot undo a failed one.
/// </summary>
public sealed class MigrationInProgressMarkerTests : IDisposable
{
    private const string MarkerFileName = "migration-in-progress.json";
    private const string NonTransactionalKey = "Jellyfin-NonTransactional";
    private const string RestoreInstruction = "restore the database from the backup taken before the upgrade";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly TestDatabase _database = TestDatabaseEnvironment.Backend.CreateEmpty();
    private readonly IJellyfinDatabaseProvider _provider;
    private readonly CapturingLoggerProvider _logs = new();
    private readonly CommandObserver _observer;
    private readonly FaultInjector _faults;
    private readonly List<ServiceProvider> _serviceProviders = [];

    public MigrationInProgressMarkerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-migration-marker-tests", Guid.NewGuid().ToString("N"));
        _paths = new ServerApplicationPaths(
            Path.Combine(_root, "data"),
            Path.Combine(_root, "log"),
            Path.Combine(_root, "config"),
            Path.Combine(_root, "cache"),
            Path.Combine(_root, "web"));
        Directory.CreateDirectory(_paths.DataPath);
        Directory.CreateDirectory(_paths.LogDirectoryPath);
        Directory.CreateDirectory(_paths.ConfigurationDirectoryPath);
        Directory.CreateDirectory(_paths.CachePath);

        _observer = new CommandObserver(MarkerPath);
        _faults = new FaultInjector(MarkerPath);
        _provider = _database.CreateProvider();
        _provider.DbContextFactory = new TestDbContextFactory(CreateDbContext);
    }

    private string MarkerPath => Path.Combine(_paths.DataPath, MarkerFileName);

    [Fact]
    public async Task MigrateStepAsync_ProviderWithoutTransactionalMigrations_NamesEachSchemaMigrationWhileItRunsAndLeavesNoFile()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var applied = await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.True(applied);
        Assert.False(File.Exists(MarkerPath));
        var schemaMigrations = GetSchemaMigrationIds();
        Assert.NotEmpty(schemaMigrations);
        Assert.Equal(schemaMigrations, _observer.MarkedMigrations.Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task MigrateStepAsync_FailingSchemaMigration_LeavesAMarkerThatNamesIt()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        var before = DateTime.UtcNow;

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        var after = DateTime.UtcNow;
        using var marker = JsonDocument.Parse(await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken));
        Assert.Equal(firstMigration, marker.RootElement.GetProperty("MigrationId").GetString());
        Assert.Equal(NonTransactionalKey, marker.RootElement.GetProperty("ProviderKey").GetString());
        var startedAt = marker.RootElement.GetProperty("StartedAtUtc").GetDateTime();
        Assert.Equal(DateTimeKind.Utc, startedAt.Kind);
        Assert.InRange(startedAt, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
    }

    [Fact]
    public async Task MigrateStepAsync_FailingSchemaMigration_LogsWhereTheMarkerIsAndWhatToDo()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();
        var firstMigration = GetSchemaMigrationIds()[0];

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        var logged = Assert.Single(CriticalMessages(), message => message.Contains(MarkerPath, StringComparison.Ordinal));
        Assert.Contains(firstMigration, logged, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, logged, StringComparison.Ordinal);
        Assert.Contains("then delete", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrateStepAsync_MarkerCannotBeDeletedAfterTheMigrationWasApplied_SaysSoAndDoesNotRollBack()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var firstMigration = GetSchemaMigrationIds()[0];
        _faults.ReplaceMarkerWithDirectoryWhenRecorded(HistoryRepository.DefaultTableName);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains(MarkerPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("was applied", exception.Message, StringComparison.Ordinal);
        Assert.Contains("could not be deleted", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Equal(exception.Message, Assert.Single(CriticalMessages()));
        Assert.DoesNotContain(_logs.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Equal([firstMigration], await GetAppliedMigrationIdsAsync(firstMigration));
        Assert.True(Directory.Exists(MarkerPath));
    }

    [Fact]
    public async Task MigrateStepAsync_MigrationLeavesNoHistoryRow_KeepsTheMarkerAndDoesNotRepeat()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var firstMigration = GetSchemaMigrationIds()[0];
        _faults.SuppressHistoryRows(HistoryRepository.DefaultTableName);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains("did not record itself", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
        using var marker = JsonDocument.Parse(await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken));
        Assert.Equal(firstMigration, marker.RootElement.GetProperty("MigrationId").GetString());
    }

    [Fact]
    public async Task MigrateStepAsync_MarkerCannotBeWritten_DoesNotStartTheMigrationAndSaysSo()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var schemaMigrations = GetSchemaMigrationIds();
        Directory.CreateDirectory(MarkerPath);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.True(exception is IOException or UnauthorizedAccessException);
        var logged = Assert.Single(CriticalMessages());
        Assert.Contains(MarkerPath, logged, StringComparison.Ordinal);
        Assert.Contains(schemaMigrations[0], logged, StringComparison.Ordinal);
        Assert.Contains("was not started", logged, StringComparison.Ordinal);
        Assert.Empty(await GetAppliedMigrationIdsAsync(schemaMigrations));
        Assert.True(Directory.Exists(MarkerPath));
    }

    [Fact]
    public async Task MigrateStepAsync_ProviderWithTransactionalMigrations_NeverWritesTheMarker()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(_provider);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var applied = await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.True(_provider.SupportsTransactionalMigrations);
        Assert.True(applied);
        Assert.NotEmpty(GetSchemaMigrationIds());
        Assert.True(_observer.CommandCount > 0);
        Assert.Empty(_observer.MarkedMigrations);
        Assert.False(File.Exists(MarkerPath));
    }

    [Fact]
    public async Task MigrateStepAsync_FailingSchemaMigrationOnAProviderWithTransactionalMigrations_LeavesNoMarker()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(_provider);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Empty(_observer.MarkedMigrations);
        Assert.False(File.Exists(MarkerPath));
        Assert.DoesNotContain(CriticalMessages(), message => message.Contains(MarkerFileName, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        _logs.Dispose();
        _database.Dispose();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private void WriteServerConfiguration(bool wizardCompleted)
    {
        new MyXmlSerializer().SerializeToFile(new ServerConfiguration { IsStartupWizardCompleted = wizardCompleted }, _paths.SystemConfigurationFilePath);
    }

    private JellyfinDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>(_database.Options)
            .AddInterceptors(_observer, _faults)
            .Options;
        return new JellyfinDbContext(options, NullLogger<JellyfinDbContext>.Instance, _provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private JellyfinMigrationService CreateService(IJellyfinDatabaseProvider provider)
    {
        var contextFactory = new TestDbContextFactory(CreateDbContext);
        var serviceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(_logs))
            .AddSingleton<IDbContextFactory<JellyfinDbContext>>(contextFactory)
            .AddSingleton<IApplicationPaths>(_paths)
            .RegisterStartupLogger()
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

        return new JellyfinMigrationService(
            contextFactory,
            serviceProvider.GetRequiredService<ILoggerFactory>(),
            serviceProvider.GetRequiredService<IStartupLogger<JellyfinMigrationService>>(),
            _paths,
            jellyfinDatabaseProvider: provider);
    }

    private async Task CreateConflictingTableAsync()
    {
        await using var context = CreateDbContext();
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier("ActivityLogs");
        var statement = "CREATE TABLE " + table + " (Id integer)";
        await context.Database.ExecuteSqlRawAsync(statement, TestContext.Current.CancellationToken);
    }

    private string[] CriticalMessages()
        => [.. _logs.Entries.Where(entry => entry.Level == LogLevel.Critical).Select(entry => entry.Message)];

    private string[] GetSchemaMigrationIds()
    {
        using var context = CreateDbContext();
        return [.. context.GetService<IMigrationsAssembly>().Migrations.Keys.Order(StringComparer.Ordinal)];
    }

    private async Task<string[]> GetAppliedMigrationIdsAsync(params string[] among)
    {
        await using var context = CreateDbContext();
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        return [.. applied.Where(among.Contains).Order(StringComparer.Ordinal)];
    }

    [JellyfinDatabaseProviderKey(NonTransactionalKey)]
    private sealed class NonTransactionalProvider : IJellyfinDatabaseProvider
    {
        private readonly IJellyfinDatabaseProvider _inner;

        public NonTransactionalProvider(IJellyfinDatabaseProvider inner)
        {
            _inner = inner;
        }

        public IDbContextFactory<JellyfinDbContext>? DbContextFactory
        {
            get => _inner.DbContextFactory;
            set => _inner.DbContextFactory = value;
        }

        public bool SupportsTransactionalMigrations => false;

        public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
            => _inner.Initialise(options, databaseConfiguration);

        public void OnModelCreating(ModelBuilder modelBuilder) => _inner.OnModelCreating(modelBuilder);

        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) => _inner.ConfigureConventions(configurationBuilder);

        public Task RunScheduledOptimisation(CancellationToken cancellationToken) => _inner.RunScheduledOptimisation(cancellationToken);

        public Task RunShutdownTask(CancellationToken cancellationToken) => _inner.RunShutdownTask(cancellationToken);

        public Task<string> MigrationBackupFast(CancellationToken cancellationToken) => _inner.MigrationBackupFast(cancellationToken);

        public Task RestoreBackupFast(string key, CancellationToken cancellationToken) => _inner.RestoreBackupFast(key, cancellationToken);

        public Task DeleteBackup(string key) => _inner.DeleteBackup(key);

        public Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames) => _inner.PurgeDatabase(dbContext, tableNames);
    }

    /// <summary>
    /// Interferes with the statement that records a schema migration as applied.
    /// </summary>
    private sealed class FaultInjector : DbCommandInterceptor
    {
        private readonly string _markerPath;
        private string? _replaceMarkerWhenRecordedIn;
        private string? _suppressRecordsIn;

        public FaultInjector(string markerPath)
        {
            _markerPath = markerPath;
        }

        public void ReplaceMarkerWithDirectoryWhenRecorded(string historyTable) => _replaceMarkerWhenRecordedIn = historyTable;

        public void SuppressHistoryRows(string historyTable) => _suppressRecordsIn = historyTable;

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            => Intercept(command, result);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Intercept(command, result));

        private static bool IsRecordIn(DbCommand command, string? historyTable)
            => historyTable is not null
                && command.CommandText.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains(historyTable, StringComparison.Ordinal);

        private InterceptionResult<int> Intercept(DbCommand command, InterceptionResult<int> result)
        {
            if (IsRecordIn(command, _suppressRecordsIn))
            {
                return InterceptionResult<int>.SuppressWithResult(1);
            }

            if (IsRecordIn(command, _replaceMarkerWhenRecordedIn))
            {
                File.Delete(_markerPath);
                Directory.CreateDirectory(_markerPath);
            }

            return result;
        }
    }

    /// <summary>
    /// Records, for every command the database is sent, which migration the marker file named at that moment.
    /// </summary>
    private sealed class CommandObserver : DbCommandInterceptor
    {
        private readonly string _markerPath;
        private readonly List<string> _markedMigrations = [];
        private int _commandCount;

        public CommandObserver(string markerPath)
        {
            _markerPath = markerPath;
        }

        public int CommandCount => _commandCount;

        public IReadOnlyList<string> MarkedMigrations
        {
            get
            {
                lock (_markedMigrations)
                {
                    return [.. _markedMigrations];
                }
            }
        }

        public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
        {
            Interlocked.Increment(ref _commandCount);
            if (File.Exists(_markerPath))
            {
                using var marker = JsonDocument.Parse(File.ReadAllText(_markerPath));
                lock (_markedMigrations)
                {
                    _markedMigrations.Add(marker.RootElement.GetProperty("MigrationId").GetString()!);
                }
            }

            return result;
        }
    }
}
