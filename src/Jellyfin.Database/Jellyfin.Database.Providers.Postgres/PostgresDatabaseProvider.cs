using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Configures jellyfin to use a PostgreSQL database.
/// </summary>
[JellyfinDatabaseProviderKey("Jellyfin-PostgreSQL")]
public sealed class PostgresDatabaseProvider : IJellyfinDatabaseProvider
{
    private const string BackupFolderName = "PostgreSQLBackups";
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<PostgresDatabaseProvider> _logger;
    private string? _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDatabaseProvider"/> class.
    /// </summary>
    /// <param name="applicationPaths">Service to construct the fallback when the old data path configuration is used.</param>
    /// <param name="logger">A logger.</param>
    public PostgresDatabaseProvider(IApplicationPaths applicationPaths, ILogger<PostgresDatabaseProvider> logger)
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

        // Build connection string from configuration
        var connectionString = BuildConnectionString(databaseConfiguration);
        _connectionString = connectionString;

        // Log connection string with redacted password
        var safeConnectionString = RedactPassword(connectionString);
        _logger.LogInformation("PostgreSQL connection string: {ConnectionString}", safeConnectionString);

        options
            .UseNpgsql(
                connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(GetType().Assembly.GetName().Name))
            // TODO: Remove when https://github.com/dotnet/efcore/pull/35873 is merged & released
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning))
            .AddInterceptors(new NpgsqlConnectionInterceptor(
                (ILogger)_logger,
                GetOption<int?>(customOptions, "lockTimeout", e => int.Parse(e, CultureInfo.InvariantCulture)),
                GetOption<bool?>(customOptions, "prepare", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase)),
                GetOption<int?>(customOptions, "commandTimeout", e => int.Parse(e, CultureInfo.InvariantCulture))));

        var enableSensitiveDataLogging = GetOption(customOptions, "EnableSensitiveDataLogging", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase), () => false);
        if (enableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging(enableSensitiveDataLogging);
            _logger.LogInformation("EnableSensitiveDataLogging is enabled on PostgreSQL connection");
        }
    }

    /// <summary>
    /// Builds the PostgreSQL connection string from database configuration.
    /// </summary>
    /// <param name="databaseConfiguration">The database configuration.</param>
    /// <returns>The PostgreSQL connection string.</returns>
    private string BuildConnectionString(DatabaseConfigurationOptions databaseConfiguration)
    {
        var connectionString = databaseConfiguration.CustomProviderOptions?.Options?.FirstOrDefault(o => o.Key.Equals("connectionstring", StringComparison.OrdinalIgnoreCase))?.Value;

        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Fallback to individual parameters
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "host", s => s, () => "localhost"),
            Port = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "port", int.Parse, () => 5432),
            Database = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "database", s => s, () => "jellyfin"),
            Username = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "username", s => s, () => "jellyfin"),
            Password = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "password", s => s, () => string.Empty),

            // SSL/TLS Configuration
            SslMode = ParseSslMode(GetOption(databaseConfiguration.CustomProviderOptions?.Options, "sslmode", s => s, () => "Disable") ?? "Disable"),

            // Connection Pool Settings
            Pooling = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "pooling", bool.Parse, () => true),
            MinPoolSize = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "minpoolsize", int.Parse, () => 0),
            MaxPoolSize = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "maxpoolsize", int.Parse, () => 100),
            ConnectionIdleLifetime = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "connectionidlelifetime", int.Parse, () => 300),

            // Timeout Settings
            Timeout = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "timeout", int.Parse, () => 30),
            CommandTimeout = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "commandtimeout", int.Parse, () => 30),

            // Application Name for monitoring
            ApplicationName = GetOption(databaseConfiguration.CustomProviderOptions?.Options, "applicationname", s => s, () => "Jellyfin")
        };

        return builder.ToString();
    }

    /// <summary>
    /// Parses SSL mode string to SslMode enum.
    /// </summary>
    /// <param name="mode">The SSL mode string.</param>
    /// <returns>The corresponding SslMode enum value.</returns>
    private static SslMode ParseSslMode(string mode)
    {
        return mode?.ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "allow" => SslMode.Allow,
            "prefer" => SslMode.Prefer,
            "require" => SslMode.Require,
            "verifyca" => SslMode.VerifyCA,
            "verifyfull" => SslMode.VerifyFull,
            _ => SslMode.Disable
        };
    }

    /// <summary>
    /// Redacts password from connection string for logging.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The connection string with redacted password.</returns>
    private static string RedactPassword(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Password = "***REDACTED***"
            };
            return builder.ToString();
        }
        catch
        {
            // If parsing fails, return a generic message
            return "[Connection string - password redacted]";
        }
    }

    /// <summary>
    /// Gets the stored connection string.
    /// </summary>
    /// <returns>The connection string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if connection string not available.</exception>
    private string GetConnectionString()
    {
        if (_connectionString == null)
        {
            throw new InvalidOperationException(
                "Connection string not available. Ensure Initialise() was called.");
        }

        return _connectionString;
    }

    /// <inheritdoc/>
    public Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        // PostgreSQL doesn't require specific scheduled optimization like SQLite
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public Task RunShutdownTask(CancellationToken cancellationToken)
    {
        // PostgreSQL doesn't require specific shutdown tasks like SQLite
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL doesn't require specific conventions like SQLite
    }

    /// <inheritdoc />
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        try
        {
            var key = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var backupDir = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
            Directory.CreateDirectory(backupDir);
            var backupFile = Path.Combine(backupDir, $"{key}_jellyfin.backup");

            _logger.LogInformation("Starting PostgreSQL backup to {BackupFile}", backupFile);

            var connectionString = GetConnectionString();
            var connBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            var pgDumpPath = FindPgDumpExecutable();
            if (string.IsNullOrEmpty(pgDumpPath))
            {
                throw new FileNotFoundException("pg_dump executable not found. Ensure PostgreSQL client tools are installed and in PATH.");
            }

            _logger.LogDebug("Using pg_dump at: {PgDumpPath}", pgDumpPath);

            var arguments = BuildPgDumpArguments(connBuilder, backupFile);
            var exitCode = await ExecuteProcessAsync(pgDumpPath, arguments, connBuilder.Password, cancellationToken).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"pg_dump failed with exit code {exitCode}. Check logs for details.");
            }

            if (!File.Exists(backupFile))
            {
                throw new InvalidOperationException("Backup file was not created despite pg_dump reporting success.");
            }

            var fileInfo = new FileInfo(backupFile);
            _logger.LogInformation("PostgreSQL backup completed successfully. File size: {Size:N0} bytes", fileInfo.Length);

            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PostgreSQL backup");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        try
        {
            var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.backup");

            if (!File.Exists(backupFile))
            {
                throw new FileNotFoundException($"Backup file not found: {backupFile}");
            }

            _logger.LogInformation("Starting PostgreSQL restore from {BackupFile}", backupFile);

            var connectionString = GetConnectionString();
            var connBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            await CloseAllConnectionsAsync(connBuilder, cancellationToken).ConfigureAwait(false);
            await DropAndRecreateDatabase(connBuilder, cancellationToken).ConfigureAwait(false);

            var pgRestorePath = FindPgRestoreExecutable();
            if (string.IsNullOrEmpty(pgRestorePath))
            {
                throw new FileNotFoundException("pg_restore executable not found. Ensure PostgreSQL client tools are installed and in PATH.");
            }

            _logger.LogDebug("Using pg_restore at: {PgRestorePath}", pgRestorePath);

            var arguments = BuildPgRestoreArguments(connBuilder, backupFile);
            var exitCode = await ExecuteProcessAsync(pgRestorePath, arguments, connBuilder.Password, cancellationToken).ConfigureAwait(false);

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"pg_restore failed with exit code {exitCode}. Check logs for details.");
            }

            _logger.LogInformation("PostgreSQL restore completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore PostgreSQL backup");
            throw;
        }
    }

    /// <inheritdoc />
    public Task DeleteBackup(string key)
    {
        try
        {
            var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.backup");

            if (!File.Exists(backupFile))
            {
                _logger.LogWarning("Backup file not found, cannot delete: {BackupFile}", backupFile);
                return Task.CompletedTask;
            }

            File.Delete(backupFile);
            _logger.LogInformation("Deleted backup file: {BackupFile}", backupFile);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup with key {Key}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);

        try
        {
            var tableList = tableNames.ToList();
            _logger.LogInformation("Starting database purge for {Count} tables", tableList.Count);

            await using var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                // Disable foreign key checks temporarily
                await dbContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica';").ConfigureAwait(false);

                // Truncate each table
                foreach (var tableName in tableList)
                {
                    _logger.LogDebug("Truncating table: {TableName}", tableName);

                    // Use TRUNCATE for better performance
                    // CASCADE removes dependent data from other tables
                    // RESTART IDENTITY resets sequences
                    var sql = $"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE;";

                    await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
                }

                // Re-enable foreign key checks
                await dbContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';").ConfigureAwait(false);

                // Commit transaction
                await transaction.CommitAsync().ConfigureAwait(false);

                _logger.LogInformation("Database purge completed successfully");
            }
            catch (Exception)
            {
                // Rollback on error
                await transaction.RollbackAsync().ConfigureAwait(false);

                // Ensure foreign keys are re-enabled even on error
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin';").ConfigureAwait(false);
                }
                catch
                {
                    // Ignore errors re-enabling foreign keys
                }

                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purge database");
            throw;
        }
    }

    private static string? FindPgDumpExecutable()
    {
        var candidates = new[]
        {
            "pg_dump",
            "/usr/bin/pg_dump",
            "/usr/local/bin/pg_dump",
            "/opt/homebrew/bin/pg_dump",
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (candidate == "pg_dump")
            {
                var path = FindInPath("pg_dump");
                if (path != null)
                {
                    return path;
                }
            }
        }

        return null;
    }

    private static string? FindPgRestoreExecutable()
    {
        var candidates = new[]
        {
            "pg_restore",
            "/usr/bin/pg_restore",
            "/usr/local/bin/pg_restore",
            "/opt/homebrew/bin/pg_restore",
            @"C:\Program Files\PostgreSQL\16\bin\pg_restore.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_restore.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_restore.exe",
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (candidate == "pg_restore")
            {
                var path = FindInPath("pg_restore");
                if (path != null)
                {
                    return path;
                }
            }
        }

        return null;
    }

    private static string? FindInPath(string fileName)
    {
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
        if (paths == null)
        {
            return null;
        }

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            if (OperatingSystem.IsWindows())
            {
                fullPath += ".exe";
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static string BuildPgDumpArguments(NpgsqlConnectionStringBuilder connBuilder, string outputFile)
    {
        var args = new List<string>
        {
            $"--host={connBuilder.Host}",
            $"--port={connBuilder.Port}",
            $"--username={connBuilder.Username}",
            $"--dbname={connBuilder.Database}",
            $"--file={QuoteArgument(outputFile)}",
            "--format=custom",
            "--compress=9",
            "--verbose",
            "--no-password"
        };

        return string.Join(" ", args);
    }

    private static string BuildPgRestoreArguments(NpgsqlConnectionStringBuilder connBuilder, string inputFile)
    {
        var args = new List<string>
        {
            $"--host={connBuilder.Host}",
            $"--port={connBuilder.Port}",
            $"--username={connBuilder.Username}",
            $"--dbname={connBuilder.Database}",
            QuoteArgument(inputFile),
            "--verbose",
            "--no-password",
            "--clean",
            "--if-exists",
            "--no-owner",
            "--no-privileges"
        };

        return string.Join(" ", args);
    }

    private static string QuoteArgument(string arg)
    {
        if (arg.Contains(' ', StringComparison.Ordinal) || arg.Contains('"', StringComparison.Ordinal))
        {
            return $"\"{arg.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }

        return arg;
    }

    private async Task<int> ExecuteProcessAsync(string fileName, string arguments, string? password, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(password))
        {
            startInfo.Environment["PGPASSWORD"] = password;
        }

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                outputBuilder.AppendLine(args.Data);
                _logger.LogDebug("pg output: {Output}", args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                errorBuilder.AppendLine(args.Data);
                _logger.LogWarning("pg error: {Error}", args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            _logger.LogError("Process failed with exit code {ExitCode}. Output: {Output}. Errors: {Errors}", process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
        }

        return process.ExitCode;
    }

    private async Task CloseAllConnectionsAsync(NpgsqlConnectionStringBuilder connBuilder, CancellationToken cancellationToken)
    {
        var adminConnBuilder = new NpgsqlConnectionStringBuilder(connBuilder.ToString())
        {
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(adminConnBuilder.ToString());
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = $@"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = '{connBuilder.Database}'
              AND pid <> pg_backend_pid();";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Closed all connections to database {Database}", connBuilder.Database);
    }

    private async Task DropAndRecreateDatabase(NpgsqlConnectionStringBuilder connBuilder, CancellationToken cancellationToken)
    {
        var adminConnBuilder = new NpgsqlConnectionStringBuilder(connBuilder.ToString())
        {
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(adminConnBuilder.ToString());
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var dropSql = $"DROP DATABASE IF EXISTS \"{connBuilder.Database}\";";
        await using (var cmd = new NpgsqlCommand(dropSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Dropped database {Database}", connBuilder.Database);

        var createSql = $"CREATE DATABASE \"{connBuilder.Database}\" WITH ENCODING 'UTF8';";
        await using (var cmd = new NpgsqlCommand(createSql, conn))
        {
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Created database {Database}", connBuilder.Database);
    }

    /// <summary>
    /// Gets an option value from the configuration.
    /// </summary>
    /// <typeparam name="T">The type of the option value.</typeparam>
    /// <param name="options">The collection of options.</param>
    /// <param name="key">The key to look for.</param>
    /// <param name="converter">The converter function.</param>
    /// <param name="defaultValue">The default value function.</param>
    /// <returns>The option value or default.</returns>
    private static T? GetOption<T>(ICollection<CustomDatabaseOption>? options, string key, Func<string, T> converter, Func<T>? defaultValue = null)
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
}
