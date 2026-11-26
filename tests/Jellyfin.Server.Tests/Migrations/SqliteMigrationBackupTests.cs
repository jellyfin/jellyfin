using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the backup the SQLite provider takes of jellyfin.db before migrations run.
/// </summary>
public sealed class SqliteMigrationBackupTests : IDisposable
{
    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<ServiceProvider> _serviceProviders = [];

    public SqliteMigrationBackupTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-migration-backup-tests", Guid.NewGuid().ToString("N"));
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
    }

    private string DatabasePath => Path.Combine(_paths.DataPath, "jellyfin.db");

    [Fact]
    public async Task MigrationBackupFast_WithUncheckpointedWrites_BacksUpConsistentDatabase()
    {
        var (provider, factory) = await CreateDatabaseAsync();
        await using var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);

        // The connection stays open, so the row only lives in the write-ahead log when the backup is taken.
        await context.Database.ExecuteSqlRawAsync(
            "CREATE TABLE BackupProbe (Value TEXT NOT NULL); INSERT INTO BackupProbe VALUES ('before');",
            TestContext.Current.CancellationToken);
        Assert.True(new FileInfo(DatabasePath + "-wal").Length > 0);

        var key = await provider.MigrationBackupFast(TestContext.Current.CancellationToken);

        var backupFile = GetBackupFile(key);
        Assert.Equal("ok", QueryScalar(backupFile, "PRAGMA integrity_check"));
        Assert.Equal("before", QueryScalar(backupFile, "SELECT group_concat(Value) FROM BackupProbe"));
    }

    [Fact]
    public async Task RestoreBackupFast_AfterMigrationBackupFast_RestoresBackedUpState()
    {
        var (provider, factory) = await CreateDatabaseAsync();
        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE BackupProbe (Value TEXT NOT NULL); INSERT INTO BackupProbe VALUES ('before');",
                TestContext.Current.CancellationToken);
        }

        var key = await provider.MigrationBackupFast(TestContext.Current.CancellationToken);

        await using (var context = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await context.Database.ExecuteSqlRawAsync("INSERT INTO BackupProbe VALUES ('after');", TestContext.Current.CancellationToken);
        }

        await provider.RestoreBackupFast(key, TestContext.Current.CancellationToken);

        Assert.Equal("before", QueryScalar(DatabasePath, "SELECT group_concat(Value) FROM BackupProbe"));

        await provider.DeleteBackup(key);
        Assert.False(File.Exists(GetBackupFile(key)));
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private static string? QueryScalar(string databasePath, string sql)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar() as string;
    }

    private string GetBackupFile(string key) => Path.Combine(_paths.DataPath, "SQLiteBackups", $"{key}_jellyfin.db");

    private async Task<(IJellyfinDatabaseProvider Provider, IDbContextFactory<JellyfinDbContext> Factory)> CreateDatabaseAsync()
    {
        new MyXmlSerializer().SerializeToFile(new ServerConfiguration { IsStartupWizardCompleted = false }, _paths.SystemConfigurationFilePath);

        var configurationManager = new ServerConfigurationManager(_paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build())
            .AddSingleton<IApplicationPaths>(_paths)
            .RegisterStartupLogger()
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        var provider = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
        provider.DbContextFactory = factory;

        await ActivatorUtilities.CreateInstance<JellyfinMigrationService>(serviceProvider)
            .CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        return (provider, factory);
    }
}
