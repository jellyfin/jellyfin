using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
    public void Initialise(DbContextOptionsBuilder options)
    {
        options.UseSqlite(
            $"Filename={Path.Combine(_applicationPaths.DataPath, "jellyfin.db")};Pooling=false",
            sqLiteOptions => sqLiteOptions.MigrationsAssembly(GetType().Assembly));

        ConfigureFuzzySearchFunctions($"Filename={Path.Combine(_applicationPaths.DataPath, "jellyfin.db")};Pooling=false");
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
        // Run before disposing the application
        var context = await DbContextFactory!.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
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
        var path = Path.Combine(_applicationPaths.DataPath, "jellyfin.db");
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
        if (!Directory.Exists(backupFile))
        {
            Directory.CreateDirectory(backupFile);
        }

        backupFile = Path.Combine(_applicationPaths.DataPath, $"{key}_jellyfin.db");
        File.Copy(path, backupFile);
        return Task.FromResult(key);
    }

    /// <inheritdoc />
    public Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_applicationPaths.DataPath, "jellyfin.db");
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.db");

        if (!File.Exists(backupFile))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist.");
            return Task.CompletedTask;
        }

        File.Copy(backupFile, path, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void ConfigureFuzzySearchFunctions(string connectionString)
    {
        try
        {
            // Créer une connexion pour configurer FTS5
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();

            // Drop the existing FTS5 table if it exists
            // In the long term, these lines will need to be removed
            command.CommandText = "DROP TABLE IF EXISTS item_search_fts;";
            _ = command.ExecuteNonQuery();
            command.CommandText = "DROP TRIGGER IF EXISTS baseitem_ai_fts;";
            _ = command.ExecuteNonQuery();
            command.CommandText = "DROP TRIGGER IF EXISTS baseitem_au_fts;";
            _ = command.ExecuteNonQuery();
            command.CommandText = "DROP TRIGGER IF EXISTS baseitem_ad_fts;";
            _ = command.ExecuteNonQuery();

            command.CommandText = @"
                -- Create a virtual FTS5 table for searching
                CREATE VIRTUAL TABLE IF NOT EXISTS item_search_fts USING FTS5(
                    id UNINDEXED,
                    clean_name,
                    original_title,
                    episode_title,
                    path,
                    tags,
                    genres,
                    tokenize='unicode61 remove_diacritics 0 tokenchars ''0123456789'''
                );

                -- Create triggers to keep the FTS5 table in sync with the BaseItems table

                CREATE TRIGGER IF NOT EXISTS baseitem_ai_fts AFTER INSERT ON BaseItems BEGIN
                    INSERT INTO item_search_fts(id, clean_name, original_title, tags, genres)
                    VALUES (
                        new.Id,
                        new.CleanName,
                        new.OriginalTitle,
                        new.EpisodeTitle,
                        new.Path,
                        new.Tags,
                        new.Genres
                    );
                END;

                CREATE TRIGGER IF NOT EXISTS baseitem_au_fts AFTER UPDATE ON BaseItems BEGIN
                    UPDATE item_search_fts
                    SET
                        clean_name = new.CleanName,
                        original_title = new.OriginalTitle,
                        episode_title = new.EpisodeTitle,
                        tags = new.Tags,
                        path = new.Path,
                        genres = new.Genres
                    WHERE id = new.Id;
                END;

                CREATE TRIGGER IF NOT EXISTS baseitem_ad_fts AFTER DELETE ON BaseItems BEGIN
                    DELETE FROM item_search_fts WHERE id = old.Id;
                END;
            ";

            _ = command.ExecuteNonQuery();

            // Initialize the FTS5 table with existing data
            command.CommandText = @"
                INSERT INTO item_search_fts(id, clean_name, original_title, episode_title, path, tags, genres)
                SELECT
                    b.Id,
                    b.CleanName,
                    b.OriginalTitle,
                    b.EpisodeTitle,
                    b.Path,
                    b.Tags,
                    b.Genres
                FROM BaseItems b
                WHERE b.OriginalTitle IS NOT NULL;
            ";

            _ = command.ExecuteNonQuery();

            _logger.LogInformation("Configuration of FTS5 search completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring FTS5 search");
        }
    }
}
