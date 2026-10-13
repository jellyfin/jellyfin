using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Conventions;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Configures jellyfin to use a PostgreSQL database.
/// </summary>
[JellyfinDatabaseProviderKey("Jellyfin-PostgreSQL")]
public sealed class PostgresDatabaseProvider : IJellyfinDatabaseProvider
{
    /// <summary>
    /// The key <see cref="MigrationBackupFast"/> returns, as PostgreSQL has nothing to snapshot.
    /// </summary>
    public const string NoSnapshotKey = "postgres:no-snapshot";

    private const int DefaultCommandTimeout = 60;
    private const int MaxIdentifierLength = 63;
    private static readonly TimeSpan _maintenanceCommandTimeout = TimeSpan.FromHours(1);
    private readonly ILogger<PostgresDatabaseProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDatabaseProvider"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public PostgresDatabaseProvider(ILogger<PostgresDatabaseProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        var customOptions = databaseConfiguration.CustomProviderOptions;
        if (string.IsNullOrWhiteSpace(customOptions?.ConnectionString))
        {
            throw new InvalidOperationException(
                "No PostgreSQL connection string is configured. Set CustomProviderOptions/ConnectionString in database.xml, "
                + "or the database:ConnectionString configuration key (environment variable JELLYFIN_DATABASE__CONNECTIONSTRING).");
        }

        var providerOptions = customOptions.Options;
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(customOptions.ConnectionString);

        var commandTimeout = GetOption<int?>(providerOptions, "command-timeout", e => int.Parse(e, CultureInfo.InvariantCulture));
        if (commandTimeout is not null)
        {
            connectionStringBuilder.CommandTimeout = commandTimeout.Value;
        }
        else if (!SpecifiesCommandTimeout(customOptions.ConnectionString))
        {
            connectionStringBuilder.CommandTimeout = DefaultCommandTimeout;
        }

        var maxPoolSize = GetOption<int?>(providerOptions, "max-pool-size", e => int.Parse(e, CultureInfo.InvariantCulture));
        if (maxPoolSize is not null)
        {
            connectionStringBuilder.MaxPoolSize = maxPoolSize.Value;
        }

        // Dates are stored as UTC, so server-side date functions must not depend on the time zone of the server.
        connectionStringBuilder.Timezone = "UTC";

        var loggableConnectionString = new NpgsqlConnectionStringBuilder(connectionStringBuilder.ConnectionString)
        {
            Password = null,
            SslPassword = null
        };
        _logger.LogInformation("PostgreSQL connection string: {ConnectionString}", loggableConnectionString.ConnectionString);

        options
            .UseNpgsql(
                connectionStringBuilder.ConnectionString,
                npgsqlOptions => npgsqlOptions
                    .MigrationsAssembly(GetType().Assembly)
                    .SetPostgresVersion(16, 0)
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
            .ReplaceService<IQuerySqlGeneratorFactory, NullOrderingQuerySqlGeneratorFactory>()
            .ConfigureWarnings(warnings =>
                warnings.Throw(RelationalEventId.NonTransactionalMigrationOperationWarning));

        var enableSensitiveDataLogging = GetOption(providerOptions, "EnableSensitiveDataLogging", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase));
        if (enableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
            _logger.LogInformation("EnableSensitiveDataLogging is enabled on PostgreSQL connection");
        }
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<string>()
            .HaveColumnType("text")
            .UseCollation("C");

        configurationBuilder.Conventions.Add(_ => new IdentifierLengthConvention(MaxIdentifierLength));
        configurationBuilder.Conventions.Add(_ => new NullsFirstIndexConvention());
    }

    /// <inheritdoc/>
    public bool IsRetryableWriteConflict(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected })
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            context.Database.SetCommandTimeout(_maintenanceCommandTimeout);
            await context.Database.ExecuteSqlRawAsync("VACUUM", cancellationToken).ConfigureAwait(false);

            if (await HasLibraryItemsAsync(context, cancellationToken).ConfigureAwait(false))
            {
                await context.Database.ExecuteSqlRawAsync("ANALYZE", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("Not analyzing the database, the library holds no items yet");
            }

            _logger.LogInformation("The database was optimized successfully");
        }
    }

    /// <inheritdoc/>
    public async Task RefreshStatistics(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            context.Database.SetCommandTimeout(_maintenanceCommandTimeout);

            if (!await HasLibraryItemsAsync(context, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            _logger.LogInformation("Analyzing the database");
            await context.Database.ExecuteSqlRawAsync("ANALYZE", cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task RunShutdownTask(CancellationToken cancellationToken)
    {
        NpgsqlConnection.ClearAllPools();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        if (await HasAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Migrations on PostgreSQL are applied one at a time and cannot be rolled back as a whole. Take a pg_dump "
                + "of the database before upgrading, it is the only way to return to the state before a failed upgrade.");
        }

        return NoSnapshotKey;
    }

    /// <inheritdoc/>
    public async Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        if (!string.Equals(key, NoSnapshotKey, StringComparison.Ordinal))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            return;
        }

        var lastApplied = await GetLastAppliedMigrationAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogCritical(
            "There is no snapshot to restore on PostgreSQL. The database was left at the last migration that applied "
            + "completely ({Migration}). Restore the pg_dump taken before the upgrade to return to the previous version.",
            lastApplied ?? "none");
    }

    /// <inheritdoc/>
    public Task DeleteBackup(string key)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
    {
        ArgumentNullException.ThrowIfNull(tableNames);

        var quotedTableNames = tableNames.Select(QuoteTableName).ToList();
        if (quotedTableNames.Count == 0)
        {
            return;
        }

        var truncateQuery = $"TRUNCATE TABLE {string.Join(", ", quotedTableNames)} RESTART IDENTITY CASCADE";
        await dbContext.Database.ExecuteSqlRawAsync(truncateQuery).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CompleteDatabaseRestoreAsync(JellyfinDbContext dbContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var schema = entityType.GetSchema();
            var table = StoreObjectIdentifier.Table(tableName, schema);
            var qualifiedTableName = schema is null ? QuoteIdentifier(tableName) : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(tableName)}";
            foreach (var property in entityType.GetProperties().Where(e => IsGeneratedIntegerKey(e, table)))
            {
                if (property.GetColumnName(table) is not { } column)
                {
                    continue;
                }

                // setval is not rolled back, so a failed restore only restores these sequences because the earlier TRUNCATE ... RESTART IDENTITY of the purge gave them storage that the rollback discards.
                // An empty table starts again at 1, as nothing in it holds an id yet.
                var quotedColumn = QuoteIdentifier(column);
                var setvalQuery = $"SELECT setval(pg_get_serial_sequence({{0}}, {{1}}), COALESCE(MAX({quotedColumn}), 1), MAX({quotedColumn}) IS NOT NULL) FROM {qualifiedTableName}";
                await dbContext.Database.ExecuteSqlRawAsync(setvalQuery, [qualifiedTableName, column], cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public async Task ApplyDatabaseSettingsAsync(JellyfinDbContext dbContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        // Jellyfin runs short queries, for which compiling with JIT costs more than it saves. A default stored for the
        // role also applies to pooled sessions, which reset their own settings, and to sessions behind a pooler.
        string? command = null;
        try
        {
            // The role is matched by name and never cast to regrole, which would read it back as an identifier.
            var session = (await dbContext.Database
                .SqlQueryRaw<JitSession>(
                    """
                    SELECT
                        current_setting('jit', true) AS "Jit",
                        current_database() AS "DatabaseName",
                        current_user AS "RoleName",
                        EXISTS (
                            SELECT 1
                            FROM pg_db_role_setting s
                            CROSS JOIN unnest(s.setconfig) AS c
                            WHERE s.setrole IN (0, (SELECT r.oid FROM pg_roles r WHERE r.rolname = current_user))
                              AND s.setdatabase IN (0, (SELECT d.oid FROM pg_database d WHERE d.datname = current_database()))
                              AND starts_with(c, 'jit=')
                        ) AS "HasConfiguredDefault"
                    """)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)).Single();

            if (session.Jit is null || string.Equals(session.Jit, "off", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (session.HasConfiguredDefault)
            {
                _logger.LogInformation(
                    "JIT compilation is on for role {Role} in database {Database} as configured on the server, leaving it as it is",
                    session.RoleName,
                    session.DatabaseName);
                return;
            }

            var role = QuoteIdentifier(session.RoleName);
            var database = QuoteIdentifier(session.DatabaseName);
            command = $"ALTER ROLE {role} IN DATABASE {database} SET jit = off";

            // The statement is formatted before it is sent, so braces in the name must be doubled.
            var statement = $"ALTER ROLE CURRENT_USER IN DATABASE {database} SET jit = off"
                .Replace("{", "{{", StringComparison.Ordinal)
                .Replace("}", "}}", StringComparison.Ordinal);
            await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken).ConfigureAwait(false);

            // Sessions that are already pooled still have JIT on.
            NpgsqlConnection.ClearPool((NpgsqlConnection)dbContext.Database.GetDbConnection());
            _logger.LogInformation(
                "Turned off JIT compilation for role {Role} in database {Database}, as it slows down the short queries of Jellyfin. To keep it on, run: ALTER ROLE {QuotedRole} IN DATABASE {QuotedDatabase} SET jit = on",
                session.RoleName,
                session.DatabaseName,
                role,
                database);
        }
        catch (DbException ex)
        {
            if (command is null)
            {
                _logger.LogWarning(ex, "Could not read the JIT setting, so it is left as it is. Jellyfin runs faster on PostgreSQL with JIT compilation turned off");
            }
            else
            {
                _logger.LogWarning(
                    ex,
                    "Could not turn off JIT compilation, which slows down the short queries of Jellyfin. To do so, run the following as a user who may alter the role: {Command}",
                    command);
            }
        }
    }

    private static bool IsGeneratedIntegerKey(IProperty property, StoreObjectIdentifier table)
    {
        var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        return property.ValueGenerated == ValueGenerated.OnAdd
            && (type == typeof(int) || type == typeof(long) || type == typeof(short))
            && property.GetValueGenerationStrategy(table) is NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                or NpgsqlValueGenerationStrategy.IdentityAlwaysColumn
                or NpgsqlValueGenerationStrategy.SerialColumn;
    }

    private static T? GetOption<T>(ICollection<CustomDatabaseOption>? options, string key, Func<string, T> converter)
    {
        var value = options?.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return value is null ? default : converter(value.Value);
    }

    private static bool SpecifiesCommandTimeout(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.ContainsKey("Command Timeout") || builder.ContainsKey("CommandTimeout");
    }

    private static Task<bool> HasLibraryItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Folders and the seeded placeholder exist before any library has been scanned.
        return context.BaseItems.AnyAsync(e => !e.IsFolder && e.Type != "PLACEHOLDER", cancellationToken);
    }

    private static string QuoteTableName(string tableName)
    {
        var separator = tableName.IndexOf('.', StringComparison.Ordinal);
        return separator < 0
            ? QuoteIdentifier(tableName)
            : $"{QuoteIdentifier(tableName[..separator])}.{QuoteIdentifier(tableName[(separator + 1)..])}";
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private async Task<bool> HasAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            // Without a way to look, assume there is something to lose.
            return true;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // The history also holds the code routines, which a fresh install records before the schema exists.
            var schemaMigrations = context.GetService<IMigrationsAssembly>().Migrations.Keys;
            return (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
                .Any(id => schemaMigrations.Contains(id));
        }
    }

    private async Task<string?> GetLastAppliedMigrationAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return null;
        }

        try
        {
            var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (context.ConfigureAwait(false))
            {
                return (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).LastOrDefault();
            }
        }
        catch (DbException ex)
        {
            // This runs because a migration just failed, so the database may well be unreachable.
            _logger.LogError(ex, "Could not read the applied migrations");
            return null;
        }
    }

    private sealed record JitSession(string? Jit, string DatabaseName, string RoleName, bool HasConfiguredDefault);
}
