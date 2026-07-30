using System;
using System.Collections.Generic;
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
    private string? _connectionString;

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
            Cache = GetOption(customOptions, "cache", Enum.Parse<SqliteCacheMode>, () => SqliteCacheMode.Default),
            Pooling = GetOption(customOptions, "pooling", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase), () => true),
            DefaultTimeout = GetOption(customOptions, "command-timeout", int.Parse, () => 60)
        };

        _connectionString = sqliteConnectionBuilder.ToString();

        // Log SQLite connection parameters
        _logger.LogInformation("SQLite connection string: {ConnectionString}", _connectionString);

        options
            .UseSqlite(
                _connectionString,
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
    public async Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        var context = await DbContextFactory!.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync("PRAGMA optimize", cancellationToken).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);
            await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE)", cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("jellyfin.db optimized successfully!");
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
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        var key = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
        Directory.CreateDirectory(backupFile);

        backupFile = Path.Combine(backupFile, $"{key}_jellyfin.db");
        try
        {
            var source = new SqliteConnection(GetConnectionString());
            await using (source.ConfigureAwait(false))
            {
                var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = backupFile,
                    Pooling = false
                }.ToString());
                await using (destination.ConfigureAwait(false))
                {
                    await source.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    source.BackupDatabase(destination);
                    return key;
                }
            }
        }
        catch
        {
            TryDeleteFile(backupFile);
            throw;
        }
    }

    /// <inheritdoc />
    public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetDatabasePath();
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.db");

        if (!File.Exists(backupFile))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            throw new FileNotFoundException($"SQLite backup '{key}' does not exist.", backupFile);
        }

        var restoreId = Guid.NewGuid().ToString("N");
        var stagedFile = $"{path}.{restoreId}.restore";
        var rollbackFile = $"{path}.{restoreId}.rollback";
        var hadDatabase = File.Exists(path);

        // Ensure there are absolutely no dangling SQLite connections before replacing the database.
        SqliteConnection.ClearAllPools();
        try
        {
            File.Copy(backupFile, stagedFile);
            if (hadDatabase)
            {
                File.Move(path, rollbackFile);
            }

            try
            {
                File.Move(stagedFile, path, true);

                // Delete SHM first. If deleting WAL fails, the previous database can still be restored
                // and SQLite will safely recreate SHM from the retained WAL.
                File.Delete(path + "-shm");
                File.Delete(path + "-wal");
            }
            catch (Exception restoreException)
            {
                try
                {
                    File.Delete(path);
                    if (hadDatabase)
                    {
                        File.Move(rollbackFile, path, true);
                    }
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException("SQLite restore and rollback both failed.", restoreException, rollbackException);
                }

                throw;
            }

            if (hadDatabase)
            {
                TryDeleteFile(rollbackFile);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDeleteFile(stagedFile);
        }

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

    private string GetConnectionString()
        => _connectionString ?? throw new InvalidOperationException("SQLite database provider has not been initialized.");

    private string GetDatabasePath()
    {
        var dataSource = new SqliteConnectionStringBuilder(GetConnectionString()).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new InvalidOperationException("SQLite connection string has no DataSource.");
        }

        return Path.GetFullPath(dataSource);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to delete SQLite restore file {Path}", path);
        }
    }
}
