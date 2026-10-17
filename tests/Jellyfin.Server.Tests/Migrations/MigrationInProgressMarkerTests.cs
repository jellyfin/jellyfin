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
using Jellyfin.Server.Implementations.SystemBackupService;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.SystemBackupService;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the file that records which schema migration is running on a provider that cannot undo a failed one.
/// </summary>
public sealed class MigrationInProgressMarkerTests : IDisposable
{
    private const string MarkerFileName = "migration-in-progress.json";
    private const string NonTransactionalKey = "Jellyfin-NonTransactional";
    private const string RestoreInstruction = "restore the database from the backup your database engine took before the upgrade";
    private const string EngineBackupExample = "mysqldump";

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

    [SchemaMigrationsFact]
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

    [SchemaMigrationsFact]
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

    [SchemaMigrationsFact]
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
        Assert.Contains(EngineBackupExample, logged, StringComparison.Ordinal);
        Assert.Contains("then delete", logged, StringComparison.Ordinal);
        Assert.Contains("does not repair the database", logged, StringComparison.Ordinal);
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_FailingSchemaMigrationOnAProviderThatTookNoSnapshot_DoesNotAskForARollbackAndSaysWhatToDo()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var provider = new NonTransactionalProvider(_provider, snapshotKey: string.Empty);
        var service = CreateService(provider, CreateBackupServiceMock());
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await service.PrepareSystemForMigration(NullLogger.Instance);
        await CreateConflictingTableAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Empty(provider.RestoredKeys);
        Assert.DoesNotContain(_logs.Entries, entry => entry.Message.Contains("rollback JellyfinDb", StringComparison.Ordinal));
        var logged = Assert.Single(CriticalMessages(), message => message.Contains(MarkerPath, StringComparison.Ordinal));
        Assert.Contains("takes no snapshot", logged, StringComparison.Ordinal);
        Assert.Contains("did not try to roll the database back", logged, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, logged, StringComparison.Ordinal);
        Assert.Contains(EngineBackupExample, logged, StringComparison.Ordinal);
        Assert.DoesNotContain(CriticalMessages(), message => message.Contains("Could not rollback", StringComparison.Ordinal));
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_FailingSchemaMigrationOnAProviderThatTookASnapshot_RollsBackFromItAndSaysWhatElseToDo()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var provider = new NonTransactionalProvider(_provider, snapshotKey: "snapshot-1");
        var service = CreateService(provider, CreateBackupServiceMock());
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await service.PrepareSystemForMigration(NullLogger.Instance);
        await CreateConflictingTableAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Equal(["snapshot-1"], provider.RestoredKeys);
        var logged = Assert.Single(CriticalMessages(), message => message.Contains(MarkerPath, StringComparison.Ordinal));
        Assert.Contains("the log above says whether that succeeded", logged, StringComparison.Ordinal);
        Assert.DoesNotContain("takes no snapshot", logged, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, logged, StringComparison.Ordinal);
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_FailingSchemaMigrationOnAProviderWithTransactionalMigrations_StillAsksForTheRollbackWithAnEmptyKey()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var provider = new NonTransactionalProvider(_provider, snapshotKey: string.Empty, transactional: true);
        var service = CreateService(provider, CreateBackupServiceMock());
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await service.PrepareSystemForMigration(NullLogger.Instance);
        await CreateConflictingTableAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Equal([string.Empty], provider.RestoredKeys);
        Assert.False(File.Exists(MarkerPath));
    }

    [SchemaMigrationsFact]
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

    [SchemaMigrationsFact]
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

    [SchemaMigrationsFact]
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

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_ProviderWithTransactionalMigrations_NeverWritesTheMarker()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var provider = CreateTransactionalProvider();
        var service = CreateService(provider);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var applied = await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.True(provider.SupportsTransactionalMigrations);
        Assert.True(applied);
        Assert.NotEmpty(GetSchemaMigrationIds());
        Assert.True(_observer.CommandCount > 0);
        Assert.Empty(_observer.MarkedMigrations);
        Assert.False(File.Exists(MarkerPath));
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_FailingSchemaMigrationOnAProviderWithTransactionalMigrations_LeavesNoMarker()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(CreateTransactionalProvider());
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Empty(_observer.MarkedMigrations);
        Assert.False(File.Exists(MarkerPath));
        Assert.DoesNotContain(CriticalMessages(), message => message.Contains(MarkerFileName, StringComparison.Ordinal));
    }

    [SchemaMigrationsFact]
    public async Task CheckFirstTimeRunOrMigration_AfterFailedSchemaMigration_RefusesToStartAndNamesTheMigration()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var failedRun = CreateService(new NonTransactionalProvider(_provider));
        await failedRun.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        await Assert.ThrowsAnyAsync<Exception>(() => failedRun.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));
        var markerBefore = await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken);
        var criticalBefore = CriticalMessages().Length;
        WriteServerConfiguration(wizardCompleted: true);
        var nextStartup = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => nextStartup.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Contains(MarkerPath, exception.Message, StringComparison.Ordinal);
        var logged = Assert.Single(CriticalMessages().Skip(criticalBefore));
        Assert.Equal(exception.Message, logged);
        Assert.Equal(markerBefore, await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken));
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public async Task MigrateStepAsync_SchemaMigrationFailingPartwayOnARealServer_LeavesAMarkerThatNamesItAndTheNextStartupRefuses()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var failedRun = CreateService(_provider);
        await failedRun.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var firstMigration = GetSchemaMigrationIds()[0];
        var operations = GetUpOperations(firstMigration);
        var created = operations.OfType<CreateTableOperation>().Select(operation => operation.Name).ToList();
        var laterIndex = operations.OfType<CreateIndexOperation>().First().Name;
        await CreateConflictingTableAsync(created[^1]);

        await Assert.ThrowsAnyAsync<Exception>(() => failedRun.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.False(_provider.SupportsTransactionalMigrations);
        using (var marker = JsonDocument.Parse(await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken)))
        {
            Assert.Equal(firstMigration, marker.RootElement.GetProperty("MigrationId").GetString());
            Assert.Equal("Jellyfin-MySQL", marker.RootElement.GetProperty("ProviderKey").GetString());
        }

        var conflicting = created[^1];
        Assert.Equal(created.Take(created.Count - 1).Order(StringComparer.Ordinal), (await GetTablesAsync()).Where(table => table != conflicting).Order(StringComparer.Ordinal));
        Assert.Equal(0, await CountIndexesAsync(laterIndex));
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
        var markerBefore = await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken);
        WriteServerConfiguration(wizardCompleted: true);
        var nextStartup = CreateService(_provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => nextStartup.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Contains(MarkerPath, exception.Message, StringComparison.Ordinal);
        Assert.Equal(markerBefore, await File.ReadAllTextAsync(MarkerPath, TestContext.Current.CancellationToken));
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_AfterFailedSchemaMigration_RefusesToMigrateAndLeavesTheDatabaseAlone()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var failedRun = CreateService(new NonTransactionalProvider(_provider));
        await failedRun.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        await Assert.ThrowsAnyAsync<Exception>(() => failedRun.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));
        var nextStartup = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => nextStartup.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(MarkerPath));
        Assert.Empty(await GetAppliedMigrationIdsAsync(firstMigration));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_MarkerNamesAnotherProvider_RefusesAndNamesBothProviders()
    {
        WriteServerConfiguration(wizardCompleted: false);
        WriteMarker("20990101000000_Unfinished", "Jellyfin-Elsewhere");
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("20990101000000_Unfinished", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Jellyfin-Elsewhere", exception.Message, StringComparison.Ordinal);
        Assert.Contains(NonTransactionalKey, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Single(CriticalMessages(), message => message == exception.Message);
        Assert.Equal(0, _observer.CommandCount);
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_StrayMarkerOnProviderWithTransactionalMigrations_Refuses()
    {
        WriteServerConfiguration(wizardCompleted: false);
        WriteMarker("20990101000000_Unfinished", "Jellyfin-Elsewhere");
        var provider = CreateTransactionalProvider();
        var service = CreateService(provider);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.True(provider.SupportsTransactionalMigrations);
        Assert.Contains("20990101000000_Unfinished", exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, _observer.CommandCount);
    }

    [Fact]
    public async Task MigrateStepAsync_StrayMarkerOnProviderWithTransactionalMigrations_RefusesBeforeTouchingTheDatabase()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(_provider);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var commandsBefore = _observer.CommandCount;
        WriteMarker("20990101000000_Unfinished", NonTransactionalKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Equal(commandsBefore, _observer.CommandCount);
        Assert.True(File.Exists(MarkerPath));
    }

    [SchemaMigrationsTheory]
    [InlineData(null)]
    [InlineData("backup.zip")]
    public async Task CheckFirstTimeRunOrMigration_MarkerNamesAMigrationThatIsRecorded_DeletesItAndContinues(string? restoreArchive)
    {
        await MigrateEverythingAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        WriteMarker(firstMigration, NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));
        var criticalBefore = CriticalMessages().Length;

        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions { RestoreArchive = restoreArchive });

        Assert.False(File.Exists(MarkerPath));
        var warning = Assert.Single(_logs.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains(MarkerPath, StringComparison.Ordinal));
        Assert.Contains(firstMigration, warning.Message, StringComparison.Ordinal);
        Assert.Equal(criticalBefore, CriticalMessages().Length);
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_MarkerCouldNotBeDeletedAfterTheMigrationWasApplied_DeletesItAndDoesNotRepeatIt()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var failedRun = CreateService(new NonTransactionalProvider(_provider));
        await failedRun.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var firstMigration = GetSchemaMigrationIds()[0];
        _faults.ReplaceMarkerWithDirectoryWhenRecorded(HistoryRepository.DefaultTableName);
        await Assert.ThrowsAsync<InvalidOperationException>(() => failedRun.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));
        _faults.Reset();
        Directory.Delete(MarkerPath);
        WriteMarker(firstMigration, NonTransactionalKey);
        var criticalBefore = CriticalMessages().Length;
        var nextStartup = CreateService(new NonTransactionalProvider(_provider));

        await nextStartup.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.False(File.Exists(MarkerPath));
        Assert.Single(_logs.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains(firstMigration, StringComparison.Ordinal));
        Assert.Equal(criticalBefore, CriticalMessages().Length);
        var schemaMigrations = GetSchemaMigrationIds();
        Assert.Equal(schemaMigrations, await GetAppliedMigrationIdsAsync(schemaMigrations));
    }

    [SchemaMigrationsFact]
    public async Task CheckFirstTimeRunOrMigration_MarkerNamesARecordedMigrationButCannotBeDeleted_RefusesAndSaysSo()
    {
        await MigrateEverythingAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        WriteMarker(firstMigration, NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));
        _faults.ReplaceMarkerWithDirectoryOnNextCommand();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains(firstMigration, exception.Message, StringComparison.Ordinal);
        Assert.Contains(MarkerPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("was applied", exception.Message, StringComparison.Ordinal);
        Assert.Contains("could not be deleted", exception.Message, StringComparison.Ordinal);
        Assert.Equal(exception.Message, Assert.Single(CriticalMessages()));
        Assert.True(Directory.Exists(MarkerPath));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_MarkerNamesAMigrationThatIsNotRecorded_RefusesEvenThoughOthersAre()
    {
        await MigrateEverythingAsync();
        WriteMarker("20990101000000_Unfinished", NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("20990101000000_Unfinished", exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(MarkerPath));
        Assert.DoesNotContain(_logs.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task MigrateStepAsync_MarkerNamesAMigrationThatIsNotRecorded_RefusesEvenThoughOthersAre()
    {
        await MigrateEverythingAsync();
        WriteMarker("20990101000000_Unfinished", NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        Assert.Contains("20990101000000_Unfinished", exception.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(MarkerPath));
    }

    [SchemaMigrationsFact]
    public async Task CheckFirstTimeRunOrMigration_MarkerNamesARecordedMigrationOfAnotherProvider_Refuses()
    {
        await MigrateEverythingAsync();
        var firstMigration = GetSchemaMigrationIds()[0];
        WriteMarker(firstMigration, "Jellyfin-Elsewhere");
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("Jellyfin-Elsewhere", exception.Message, StringComparison.Ordinal);
        Assert.Contains(NonTransactionalKey, exception.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(MarkerPath));
    }

    [SchemaMigrationsFact]
    public async Task CheckFirstTimeRunOrMigration_MarkerWithoutAMigratedDatabase_RefusesAndCreatesNothing()
    {
        WriteServerConfiguration(wizardCompleted: true);
        WriteMarker(GetSchemaMigrationIds()[0], NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(MarkerPath));
        await using var context = CreateDbContext();
        Assert.False(await context.GetService<IRelationalDatabaseCreator>().HasTablesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_RestoringAnArchiveWhileMarked_SaysTheArchiveCannotRepairTheSchema()
    {
        WriteServerConfiguration(wizardCompleted: true);
        WriteMarker("20990101000000_Unfinished", NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions { RestoreArchive = "backup.zip" }));

        Assert.Contains("--restore-archive", exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not repair the database", exception.Message, StringComparison.Ordinal);
        Assert.Contains("--mode MigrateSystem", exception.Message, StringComparison.Ordinal);
        Assert.Contains(EngineBackupExample, exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Equal(exception.Message, Assert.Single(CriticalMessages()));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_MarkedWithoutRestoringAnArchive_DoesNotMentionArchives()
    {
        WriteServerConfiguration(wizardCompleted: true);
        WriteMarker("20990101000000_Unfinished", NonTransactionalKey);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.DoesNotContain("--restore-archive", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task CheckFirstTimeRunOrMigration_MarkerCannotBeRead_RefusesAndSaysSo(string content)
    {
        WriteServerConfiguration(wizardCompleted: false);
        await File.WriteAllTextAsync(MarkerPath, content, TestContext.Current.CancellationToken);
        var service = CreateService(new NonTransactionalProvider(_provider));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains(MarkerPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be read", exception.Message, StringComparison.Ordinal);
        Assert.Contains(RestoreInstruction, exception.Message, StringComparison.Ordinal);
        Assert.Contains(CriticalMessages(), message => message == exception.Message);
        Assert.Equal(0, _observer.CommandCount);
    }

    [SchemaMigrationsFact]
    public async Task MigrateStepAsync_MarkerDeletedAfterRestore_MigratesAndLeavesNoMarker()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var failedRun = CreateService(new NonTransactionalProvider(_provider));
        await failedRun.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await CreateConflictingTableAsync();
        await Assert.ThrowsAnyAsync<Exception>(() => failedRun.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));
        await DropConflictingTableAsync();
        File.Delete(MarkerPath);
        var nextStartup = CreateService(new NonTransactionalProvider(_provider));
        await nextStartup.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var applied = await nextStartup.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.True(applied);
        Assert.False(File.Exists(MarkerPath));
        var schemaMigrations = GetSchemaMigrationIds();
        Assert.Equal(schemaMigrations, await GetAppliedMigrationIdsAsync(schemaMigrations));
    }

    [Fact]
    public async Task MigrateStepAsync_NoMarker_LogsNoCriticalEntry()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(_provider);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.DoesNotContain(_logs.Entries, entry => entry.Level >= LogLevel.Error);
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

    private static IBackupService CreateBackupServiceMock()
    {
        var backupService = new Mock<IBackupService>();
        backupService
            .Setup(e => e.CreateBackupAsync(It.IsAny<BackupOptionsDto>()))
            .ReturnsAsync(new BackupManifestDto
            {
                ServerVersion = new Version(1, 0),
                BackupEngineVersion = new Version(1, 0),
                DateCreated = DateTimeOffset.UtcNow,
                Path = Path.Combine(Path.GetTempPath(), "jellyfin-no-such-backup.zip"),
                Options = new BackupOptionsDto()
            });
        return backupService.Object;
    }

    private JellyfinMigrationService CreateService(IJellyfinDatabaseProvider provider, IBackupService? backupService = null)
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
            backupService,
            provider);
    }

    private IJellyfinDatabaseProvider CreateTransactionalProvider()
        => _provider.SupportsTransactionalMigrations ? _provider : new NonTransactionalProvider(_provider, transactional: true);

    private async Task CreateConflictingTableAsync(string name = "ActivityLogs")
    {
        await using var context = CreateDbContext();
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier(name);
        var statement = "CREATE TABLE " + table + " (Id integer)";
        await context.Database.ExecuteSqlRawAsync(statement, TestContext.Current.CancellationToken);
    }

    private async Task DropConflictingTableAsync()
    {
        await using var context = CreateDbContext();
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier("ActivityLogs");
        var statement = "DROP TABLE " + table;
        await context.Database.ExecuteSqlRawAsync(statement, TestContext.Current.CancellationToken);
    }

    private async Task MigrateEverythingAsync()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService(new NonTransactionalProvider(_provider));
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);
        WriteServerConfiguration(wizardCompleted: true);
    }

    private void WriteMarker(string migrationId, string providerKey)
    {
        var marker = new { MigrationId = migrationId, ProviderKey = providerKey, StartedAtUtc = DateTime.UtcNow };
        File.WriteAllText(MarkerPath, JsonSerializer.Serialize(marker));
    }

    private string[] CriticalMessages()
        => [.. _logs.Entries.Where(entry => entry.Level == LogLevel.Critical).Select(entry => entry.Message)];

    private List<MigrationOperation> GetUpOperations(string migrationId)
    {
        using var context = CreateDbContext();
        var assembly = context.GetService<IMigrationsAssembly>();
        return [.. assembly.CreateMigration(assembly.Migrations[migrationId], context.Database.ProviderName!).UpOperations];
    }

    private async Task<List<string>> GetTablesAsync()
    {
        await using var context = CreateDbContext();
        return await context.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS `Value` FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME <> '__EFMigrationsHistory'")
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> CountIndexesAsync(string name)
    {
        await using var context = CreateDbContext();
        return await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS `Value` FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND INDEX_NAME = {name}")
            .SingleAsync(TestContext.Current.CancellationToken);
    }

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
        private readonly string? _snapshotKey;
        private readonly bool _transactional;

        /// <summary>
        /// Initializes a new instance of the <see cref="NonTransactionalProvider"/> class.
        /// </summary>
        /// <param name="inner">The provider that does the work.</param>
        /// <param name="snapshotKey">The key that the provider reports for its snapshot, where the empty key means that none was taken. The snapshot of the inner provider when null.</param>
        /// <param name="transactional">A value indicating whether the provider reports that it undoes a failed migration.</param>
        public NonTransactionalProvider(IJellyfinDatabaseProvider inner, string? snapshotKey = null, bool transactional = false)
        {
            _inner = inner;
            _snapshotKey = snapshotKey;
            _transactional = transactional;
        }

        public List<string> RestoredKeys { get; } = [];

        public IDbContextFactory<JellyfinDbContext>? DbContextFactory
        {
            get => _inner.DbContextFactory;
            set => _inner.DbContextFactory = value;
        }

        public bool SupportsTransactionalMigrations => _transactional;

        public bool SupportsPessimisticLocking => _inner.SupportsPessimisticLocking;

        public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
            => _inner.Initialise(options, databaseConfiguration);

        public void OnModelCreating(ModelBuilder modelBuilder) => _inner.OnModelCreating(modelBuilder);

        public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) => _inner.ConfigureConventions(configurationBuilder);

        public Task RunScheduledOptimisation(CancellationToken cancellationToken) => _inner.RunScheduledOptimisation(cancellationToken);

        public Task RunShutdownTask(CancellationToken cancellationToken) => _inner.RunShutdownTask(cancellationToken);

        public Task<string> MigrationBackupFast(CancellationToken cancellationToken)
            => _snapshotKey is null ? _inner.MigrationBackupFast(cancellationToken) : Task.FromResult(_snapshotKey);

        public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
        {
            RestoredKeys.Add(key);
            return _snapshotKey is null ? _inner.RestoreBackupFast(key, cancellationToken) : Task.CompletedTask;
        }

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
        private bool _replaceMarkerOnNextCommand;

        public FaultInjector(string markerPath)
        {
            _markerPath = markerPath;
        }

        public void ReplaceMarkerWithDirectoryWhenRecorded(string historyTable) => _replaceMarkerWhenRecordedIn = historyTable;

        public void ReplaceMarkerWithDirectoryOnNextCommand() => _replaceMarkerOnNextCommand = true;

        public void SuppressHistoryRows(string historyTable) => _suppressRecordsIn = historyTable;

        public void Reset()
        {
            _replaceMarkerWhenRecordedIn = null;
            _replaceMarkerOnNextCommand = false;
            _suppressRecordsIn = null;
        }

        public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
        {
            if (_replaceMarkerOnNextCommand)
            {
                _replaceMarkerOnNextCommand = false;
                ReplaceMarkerWithDirectory();
            }

            return result;
        }

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
                ReplaceMarkerWithDirectory();
            }

            return result;
        }

        private void ReplaceMarkerWithDirectory()
        {
            File.Delete(_markerPath);
            Directory.CreateDirectory(_markerPath);
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
            var migrationId = ReadMarkedMigration();
            if (migrationId is not null)
            {
                lock (_markedMigrations)
                {
                    _markedMigrations.Add(migrationId);
                }
            }

            return result;
        }

        private string? ReadMarkedMigration()
        {
            if (!File.Exists(_markerPath))
            {
                return null;
            }

            try
            {
                using var marker = JsonDocument.Parse(File.ReadAllText(_markerPath));
                return marker.RootElement.TryGetProperty("MigrationId", out var migrationId) ? migrationId.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
