using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDatabaseProvider"/> class.
    /// </summary>
    /// <param name="applicationPaths">Service to construct the fallback when the old data path configuration is used.</param>
    /// <param name="logger">A logger.</param>
    public SqliteDatabaseProvider(IApplicationPaths applicationPaths, ILogger<SqliteDatabaseProvider> logger)
    {
        _applicationPaths = applicationPaths;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        static T? GetOption<T>(ICollection<CustomDatabaseOption>? options, string key, Func<string, T> converter, Func<T>? defaultValue = null)
        {
            if (options is null)
            {
                return defaultValue is not null ? defaultValue() : default;
            }

            var value = options.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (value is null)
            {
                return defaultValue is not null ? defaultValue() : default;
            }

            return converter(value.Value);
        }

        var customOptions = databaseConfiguration.CustomProviderOptions?.Options;

        var sqliteConnectionBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = GetOption(customOptions, "path", e => e, () => Path.Combine(_applicationPaths.DataPath, "jellyfin.db")),
            // Private, not Default: sqlite3_enable_shared_cache is process-global, so a plugin
            // enabling it makes these connections share a cache too. Contention then surfaces as
            // SQLITE_LOCKED ("database table is locked"), which the busy handler does not cover,
            // so busy_timeout is skipped and the command fails at CommandTimeout instead.
            Cache = GetOption(customOptions, "cache", Enum.Parse<SqliteCacheMode>, () => SqliteCacheMode.Private),
            Pooling = GetOption(customOptions, "pooling", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase), () => true),
            DefaultTimeout = GetOption(customOptions, "command-timeout", int.Parse, () => 60)
        };

        var connectionString = sqliteConnectionBuilder.ToString();

        // Log SQLite connection parameters
        _logger.LogInformation("SQLite connection string: {ConnectionString}", connectionString);

        options
            .UseSqlite(
                connectionString,
                sqLiteOptions => sqLiteOptions.MigrationsAssembly(GetType().Assembly))
            // TODO: Remove when https://github.com/dotnet/efcore/pull/35873 is merged & released
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning)
                    .Ignore(RelationalEventId.MultipleCollectionIncludeWarning))
            .AddInterceptors(new PragmaConnectionInterceptor(
                _logger,
                GetOption<int?>(customOptions, "cacheSize", e => int.Parse(e, CultureInfo.InvariantCulture)),
                GetOption(customOptions, "lockingmode", e => e, () => "NORMAL")!,
                GetOption(customOptions, "journalsizelimit", int.Parse, () => 134_217_728),
                GetOption(customOptions, "tempstoremode", int.Parse, () => 2),
                GetOption(customOptions, "syncmode", int.Parse, () => 1),
                customOptions?.Where(e => e.Key.StartsWith("#PRAGMA:", StringComparison.OrdinalIgnoreCase)).ToDictionary(e => e.Key["#PRAGMA:".Length..], e => e.Value) ?? []));

        var enableSensitiveDataLogging = GetOption(customOptions, "EnableSensitiveDataLogging", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase), () => false);
        if (enableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging(enableSensitiveDataLogging);
            _logger.LogInformation("EnableSensitiveDataLogging is enabled on SQLite connection");
        }
    }

    /// <inheritdoc/>
    public Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        return OptimizeAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public async Task RunShutdownTask(CancellationToken cancellationToken)
    {
        // Run before disposing the application. Only a checkpoint: stopping is on a deadline.

        // Empty the pool first. Anything still parked in it can start reading again between here and the
        // checkpoint, and a reader that holds the write-ahead log open is exactly what makes the truncation
        // fail. Connections handed out already cannot be taken away, but they get disposed on return.
        SqliteConnection.ClearAllPools();

        try
        {
            if (DbContextFactory is not null)
            {
                var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                await using (context.ConfigureAwait(false))
                {
                    await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            // A missed checkpoint only leaves a write-ahead log for the next start to replay, so never fail the
            // shutdown over this.
            _logger.LogError(ex, "Error while checkpointing jellyfin.db");
        }

        // The checkpointing connection went back into the pool, so retire that one as well.
        SqliteConnection.ClearAllPools();
    }

    private async Task OptimizeAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                long? tempStore;
                long? analysisLimit;
                var pragmaCommand = context.Database.GetDbConnection().CreateCommand();
                await using (pragmaCommand.ConfigureAwait(false))
                {
                    pragmaCommand.CommandText = "PRAGMA temp_store";
                    tempStore = await ReadPragmaValueAsync(pragmaCommand, cancellationToken).ConfigureAwait(false);
                    pragmaCommand.CommandText = "PRAGMA analysis_limit";
                    analysisLimit = await ReadPragmaValueAsync(pragmaCommand, cancellationToken).ConfigureAwait(false);
                }

                await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Rebuilding jellyfin.db on disk, scratch space goes to {TempDirectory}",
                    Environment.GetEnvironmentVariable("SQLITE_TMPDIR") ?? "SQLite's default temporary directory");
                await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=1", cancellationToken).ConfigureAwait(false);
                try
                {
                    await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    // The connection goes back to the pool, so hand it over the way it was handed to us.
                    if (tempStore is not null)
                    {
                        await context.Database.ExecuteSqlRawAsync(
                            FormattableString.Invariant($"PRAGMA temp_store={tempStore.Value}"),
                            CancellationToken.None).ConfigureAwait(false);
                    }
                }

                await context.Database.ExecuteSqlRawAsync("PRAGMA analysis_limit=0", cancellationToken).ConfigureAwait(false);
                try
                {
                    await context.Database.ExecuteSqlRawAsync("ANALYZE", cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (analysisLimit is not null)
                    {
                        await context.Database.ExecuteSqlRawAsync(
                            FormattableString.Invariant($"PRAGMA analysis_limit={analysisLimit.Value}"),
                            CancellationToken.None).ConfigureAwait(false);
                    }
                }

                await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("jellyfin.db optimized successfully!");
            }
            finally
            {
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task<long?> ReadPragmaValueAsync(DbCommand command, CancellationToken cancellationToken)
    {
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull ? null : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DoNotUseReturningClauseConvention());
    }

    /// <inheritdoc />
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_applicationPaths.DataPath, "jellyfin.db");
        var backupFolder = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
        Directory.CreateDirectory(backupFolder);

        if (DbContextFactory is not null && File.Exists(path))
        {
            var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (context.ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);
            }
        }

        var key = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var backupFile = Path.Combine(backupFolder, $"{key}_jellyfin.db");
        for (var attempt = 1; File.Exists(backupFile); attempt++)
        {
            key = string.Create(CultureInfo.InvariantCulture, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{attempt}");
            backupFile = Path.Combine(backupFolder, $"{key}_jellyfin.db");
        }

        File.Copy(path, backupFile);
        return key;
    }

    /// <inheritdoc />
    public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        // ensure there are absolutely no dangling Sqlite connections.
        SqliteConnection.ClearAllPools();
        var path = Path.Combine(_applicationPaths.DataPath, "jellyfin.db");
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.db");

        if (!File.Exists(backupFile))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            return Task.CompletedTask;
        }

        if (!TryRetireWriteAheadLog(path))
        {
            _logger.LogCritical(
                "Refusing to restore jellyfin.db: the write-ahead log at {WriteAheadLog} could not be retired, which "
                + "means the database is still open and replacing it now would silently bring back the data this "
                + "rollback is undoing. Stop the server and copy {Backup} over {Path} by hand.",
                path + "-wal",
                backupFile,
                path);
            return Task.CompletedTask;
        }

        File.Copy(backupFile, path, true);

        return Task.CompletedTask;
    }

    private bool TryRetireWriteAheadLog(string path)
    {
        var writeAheadLogPath = path + "-wal";
        if (!File.Exists(path) || !File.Exists(writeAheadLogPath))
        {
            return true;
        }

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ToString();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            // Either something else holds the database or it is too damaged to open. The check below covers both.
            _logger.LogError(ex, "Could not open jellyfin.db to retire its write-ahead log");
        }

        return !File.Exists(writeAheadLogPath);
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
}
