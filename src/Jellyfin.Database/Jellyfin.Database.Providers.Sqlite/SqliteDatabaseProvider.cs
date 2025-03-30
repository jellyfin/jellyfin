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
            // Create a connection to register our custom functions
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // Register the tokenization function
            connection.CreateFunction("generate_tokens", (string text) =>
            {
                if (string.IsNullOrEmpty(text))
                {
                    return string.Empty;
                }

                // Convert to lowercase and remove non-alphanumeric characters
                text = new string([.. text.ToLower(CultureInfo.CurrentCulture).Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))]);

                // Tokenize by space
                return string.Join(" ", text.Split([' '], StringSplitOptions.RemoveEmptyEntries));
            });

            // Function to calculate a simple similarity score
            connection.CreateFunction("simple_similarity", (string s1, string s2) =>
            {
                if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                {
                    return 0.0;
                }

                s1 = s1.ToLower(CultureInfo.CurrentCulture);
                s2 = s2.ToLower(CultureInfo.CurrentCulture);

                // Partial inclusion check
                if (s1.Contains(s2, StringComparison.OrdinalIgnoreCase) || s2.Contains(s1, StringComparison.OrdinalIgnoreCase))
                {
                    return 0.8;
                }

                // Check first characters (errors often towards the end)
                var prefixLen = Math.Min(Math.Min(s1.Length, s2.Length), 3);
                if (prefixLen > 0 && s1[..prefixLen] == s2[..prefixLen])
                {
                    return 0.7;
                }

                // Simple similarity calculation based on common characters
                var commonChars = 0;
                foreach (var c in s1)
                {
                    if (s2.Contains(c, StringComparison.Ordinal))
                    {
                        commonChars++;
                    }
                }

                return (double)commonChars / Math.Max(s1.Length, s2.Length);
            });

            // Function that checks if a string contains at least N tokens from the other
            connection.CreateFunction("contains_tokens", (string text, string searchTerms, int minTokens) =>
            {
                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchTerms))
                {
                    return 0;
                }

                var textTokens = text.ToLower(CultureInfo.CurrentCulture).Split([' '], StringSplitOptions.RemoveEmptyEntries);
                var searchTokens = searchTerms.ToLower(CultureInfo.CurrentCulture).Split([' '], StringSplitOptions.RemoveEmptyEntries);

                var matchCount = 0;
                foreach (var token in searchTokens)
                {
                    if (token.Length < 3)
                    {
                        // For short tokens, check exact equality
                        if (textTokens.Any(t => t.Equals(token, StringComparison.OrdinalIgnoreCase)))
                        {
                            matchCount++;
                        }
                    }
                    else
                    {
                        // For longer tokens, check if a text token contains the search token
                        if (textTokens.Any(t => t.Contains(token, StringComparison.OrdinalIgnoreCase) || token.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            matchCount++;
                        }
                    }
                }

                return matchCount >= minTokens ? 1 : 0;
            });

            // Execute SQL queries to create indexes if necessary
            using var command = connection.CreateCommand();

            // Create an index on important columns for search
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_baseitem_cleanname ON BaseItems(CleanName);
                CREATE INDEX IF NOT EXISTS idx_baseitem_originaltitle ON BaseItems(OriginalTitle);
            ";
            _ = command.ExecuteNonQuery();

            _logger.LogInformation("Configuration of fuzzy search functions completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring fuzzy search functions");
        }
    }
}
