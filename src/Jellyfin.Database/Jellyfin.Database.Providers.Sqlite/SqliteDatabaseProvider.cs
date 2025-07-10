using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// Configures jellyfin to use an SQLite database.
/// </summary>
[JellyfinDatabaseProviderKey("Jellyfin-SQLite")]
public sealed class SqliteDatabaseProvider : IJellyfinDatabaseProvider
{
    private const string BackupFolderName = "SQLiteBackups";
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<SqliteDatabaseProvider> _logger;
    private readonly DatabaseConfigurationOptions _databaseConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDatabaseProvider"/> class.
    /// </summary>
    /// <param name="applicationPaths">Service to construct the fallback when the old data path configuration is used.</param>
    /// <param name="logger">A logger.</param>
    /// <param name="databaseConfig">Database configuration options.</param>
    public SqliteDatabaseProvider(IApplicationPaths applicationPaths, ILogger<SqliteDatabaseProvider> logger, DatabaseConfigurationOptions databaseConfig)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
        _databaseConfig = databaseConfig;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options)
    {
        var databaseDirectory = _databaseConfig.DatabaseDirectory ?? _applicationPaths.DataPath;
        var databasePath = Path.Combine(databaseDirectory, "jellyfin.db");

        var pooling = _databaseConfig.SqliteOptions?.Pooling ?? false;
        var connectionString = $"Filename={databasePath};Pooling={pooling}";

        _logger.LogInformation("Opening database {DatabasePath} with pooling={Pooling}", databasePath, pooling);
        
        // Log SQLite configuration once at startup
        var sqliteOptions = _databaseConfig.SqliteOptions;
        var journalMode = sqliteOptions?.JournalMode ?? "WAL";
        var journalSizeLimit = sqliteOptions?.JournalSizeLimit ?? (128 * 1024 * 1024);
        
        _logger.LogInformation("Set SQLite PRAGMA journal_mode = {JournalMode}", journalMode);
        _logger.LogInformation("Set SQLite PRAGMA journal_size_limit = {JournalSizeLimit}", journalSizeLimit);
        
        if (sqliteOptions?.CacheSize.HasValue == true)
        {
            _logger.LogInformation("Set SQLite PRAGMA cache_size = {CacheSize}", sqliteOptions.CacheSize.Value);
        }
        
        if (sqliteOptions?.MmapSize.HasValue == true)
        {
            _logger.LogInformation("Set SQLite PRAGMA mmap_size = {MmapSize}", sqliteOptions.MmapSize.Value);
        }
        
        _logger.LogInformation("Set SQLite PRAGMA temp_store = 2 (MEMORY)");

        options
            .UseSqlite(
                connectionString,
                sqLiteOptions => sqLiteOptions.MigrationsAssembly(GetType().Assembly))
            // TODO: Remove when https://github.com/dotnet/efcore/pull/35873 is merged & released
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .AddInterceptors(new SqlitePragmaInterceptor(_databaseConfig, _logger));
    }

    /// <inheritdoc/>
    public async Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        var context = await DbContextFactory!.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
                await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("jellyfin.db optimized successfully!");
            }
            else
            {
                _logger.LogInformation("This database doesn't support optimization");
            }
        }
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public async Task RunShutdownTask(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return;
        }

        // Run before disposing the application
        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
        }

        SqliteConnection.ClearAllPools();
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DoNotUseReturningClauseConvention());
    }

    /// <inheritdoc />
    public Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        var key = DateTime.UtcNow.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture);
        var databaseDirectory = _databaseConfig.DatabaseDirectory ?? _applicationPaths.DataPath;
        var path = Path.Combine(databaseDirectory, "jellyfin.db");
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
        Directory.CreateDirectory(backupFile);

        backupFile = Path.Combine(backupFile, $"{key}_jellyfin.db");
        File.Copy(path, backupFile);
        return Task.FromResult(key);
    }

    /// <inheritdoc />
    public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        // ensure there are absolutly no dangling Sqlite connections.
        SqliteConnection.ClearAllPools();
        var databaseDirectory = _databaseConfig.DatabaseDirectory ?? _applicationPaths.DataPath;
        var path = Path.Combine(databaseDirectory, "jellyfin.db");
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.db");

        if (!File.Exists(backupFile))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            return Task.CompletedTask;
        }

        File.Copy(backupFile, path, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteBackup(string key)
    {
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.db");

        if (!File.Exists(backupFile))
        {
            _logger.LogCritical("Tried to delete a backup that does not exist: {Key}", key);
            return Task.CompletedTask;
        }

        File.Delete(backupFile);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);

        var deleteQueries = new List<string>();
        foreach (var tableName in tableNames)
        {
            deleteQueries.Add($"DELETE FROM \"{tableName}\";");
        }

        var deleteAllQuery =
        $"""
        PRAGMA foreign_keys = OFF;
        {string.Join('\n', deleteQueries)}
        PRAGMA foreign_keys = ON;
        """;

        await dbContext.Database.ExecuteSqlRawAsync(deleteAllQuery).ConfigureAwait(false);
    }

    private sealed class SqlitePragmaInterceptor : DbConnectionInterceptor
    {
        private readonly DatabaseConfigurationOptions _databaseConfig;
        private readonly ILogger<SqliteDatabaseProvider> _logger;

        public SqlitePragmaInterceptor(DatabaseConfigurationOptions databaseConfig, ILogger<SqliteDatabaseProvider> logger)
        {
            _databaseConfig = databaseConfig;
            _logger = logger;
        }

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (connection is SqliteConnection sqliteConnection)
            {
                ApplyPragmaSettings(sqliteConnection);
            }

            base.ConnectionOpened(connection, eventData);
        }

        public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (connection is SqliteConnection sqliteConnection)
            {
                await ApplyPragmaSettingsAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);
            }

            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
        }

        private void ApplyPragmaSettings(SqliteConnection connection)
        {
            var sqliteOptions = _databaseConfig.SqliteOptions;

            using var command = connection.CreateCommand();

#pragma warning disable CA2100 // Review if the query string passed to 'string SqliteCommand.CommandText' accepts any user input
            // SQLite PRAGMA processor is not susceptible to SQL injection. It only processes one PRAGMA command and does not allow multiple commands separated by ;.
            // Set journal mode (default WAL)
            var journalMode = sqliteOptions?.JournalMode ?? "WAL";
            command.CommandText = $"PRAGMA journal_mode = {journalMode}";
            command.ExecuteNonQuery();
            _logger.LogDebug("Set SQLite PRAGMA journal_mode = {JournalMode}", journalMode);

            // Set journal size limit (default 128MB)
            var journalSizeLimit = sqliteOptions?.JournalSizeLimit ?? (128 * 1024 * 1024);
            command.CommandText = $"PRAGMA journal_size_limit = {journalSizeLimit}";
            command.ExecuteNonQuery();
            _logger.LogDebug("Set SQLite PRAGMA journal_size_limit = {JournalSizeLimit}", journalSizeLimit);

            // Set cache size if specified (uses SQLite default if not specified)
            if (sqliteOptions?.CacheSize.HasValue == true)
            {
                command.CommandText = $"PRAGMA cache_size = {sqliteOptions.CacheSize.Value}";
                command.ExecuteNonQuery();
                _logger.LogDebug("Set SQLite PRAGMA cache_size = {CacheSize}", sqliteOptions.CacheSize.Value);
            }

            // Set mmap size if specified (uses SQLite default if not specified)
            if (sqliteOptions?.MmapSize.HasValue == true)
            {
                command.CommandText = $"PRAGMA mmap_size = {sqliteOptions.MmapSize.Value}";
                command.ExecuteNonQuery();
                _logger.LogDebug("Set SQLite PRAGMA mmap_size = {MmapSize}", sqliteOptions.MmapSize.Value);
            }

            // Set temp_store to MEMORY
            command.CommandText = "PRAGMA temp_store = 2";
            command.ExecuteNonQuery();
            _logger.LogDebug("Set SQLite PRAGMA temp_store = 2 (MEMORY)");
#pragma warning restore CA2100
        }

        private async Task ApplyPragmaSettingsAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            var sqliteOptions = _databaseConfig.SqliteOptions;

            using var command = connection.CreateCommand();

#pragma warning disable CA2100 // Review if the query string passed to 'string SqliteCommand.CommandText' accepts any user input
            // SQLite PRAGMA processor is not susceptible to SQL injection. It only processes one PRAGMA command and does not allow multiple commands separated by ;.
            // Set journal mode (default WAL)
            var journalMode = sqliteOptions?.JournalMode ?? "WAL";
            command.CommandText = $"PRAGMA journal_mode = {journalMode}";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Set SQLite PRAGMA journal_mode = {JournalMode}", journalMode);

            // Set journal size limit (default 128MB)
            var journalSizeLimit = sqliteOptions?.JournalSizeLimit ?? (128 * 1024 * 1024);
            command.CommandText = $"PRAGMA journal_size_limit = {journalSizeLimit}";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Set SQLite PRAGMA journal_size_limit = {JournalSizeLimit}", journalSizeLimit);

            // Set cache size if specified (uses SQLite default if not specified)
            if (sqliteOptions?.CacheSize.HasValue == true)
            {
                command.CommandText = $"PRAGMA cache_size = {sqliteOptions.CacheSize.Value}";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Set SQLite PRAGMA cache_size = {CacheSize}", sqliteOptions.CacheSize.Value);
            }

            // Set mmap size if specified (uses SQLite default if not specified)
            if (sqliteOptions?.MmapSize.HasValue == true)
            {
                command.CommandText = $"PRAGMA mmap_size = {sqliteOptions.MmapSize.Value}";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Set SQLite PRAGMA mmap_size = {MmapSize}", sqliteOptions.MmapSize.Value);
            }

            // Set temp_store to MEMORY
            command.CommandText = "PRAGMA temp_store = 2";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Set SQLite PRAGMA temp_store = 2 (MEMORY)");
#pragma warning restore CA2100
        }
    }
}
