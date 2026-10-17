using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Conventions;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Configures jellyfin to use a SQL Server database.
/// </summary>
[JellyfinDatabaseProviderKey("Jellyfin-SqlServer")]
public sealed class SqlServerDatabaseProvider : IJellyfinDatabaseProvider
{
    /// <summary>
    /// The collation of every text column. It compares the UTF-16 code units of the text, so that it tells every two
    /// different strings apart.
    /// </summary>
    internal const string Collation = "Latin1_General_100_BIN2";

    /// <summary>
    /// The compatibility level EF Core writes its SQL for, that of SQL Server 2022, the oldest supported version.
    /// </summary>
    internal const int CompatibilityLevel = 160;

    private const int DefaultCommandTimeout = 60;
    private const int MaxIdentifierLength = 128;

    // Errors raised with THROW or RAISERROR, as a trigger does, have numbers from this one on.
    private const int FirstUserError = 50000;

    private const string ConnectionStringGuidance =
        "Set CustomProviderOptions/ConnectionString in database.xml, "
        + "or the database:ConnectionString configuration key (environment variable JELLYFIN_DATABASE__CONNECTIONSTRING).";

    private static readonly TimeSpan _maintenanceCommandTimeout = TimeSpan.FromHours(1);

    // The server builds a provider for the migrations that run before it starts and another for the running server, and
    // each of them is set up once. The warning is given once for each database in the process, so a start shows it once.
    private static readonly ConcurrentDictionary<string, bool> _databasesWarnedOf = new(StringComparer.Ordinal);

    // The errors of the server for values it refuses: a duplicate key (2601, 2627), a missing or referenced row of a
    // foreign key, or a violated check constraint (547), a value cut (2628, 8152), a NULL in a column that takes none
    // (515), a number out of range (8115), a value of the wrong form (245, 8114), and an index key over the limit of the
    // index (1946). Errors raised with THROW or RAISERROR, as a trigger does, have numbers from 50000 on.
    private static readonly HashSet<int> _dataErrors = [245, 515, 547, 1946, 2601, 2627, 2628, 8114, 8115, 8152];

    // A duplicate key (2601, 2627), the victim of a deadlock (1205), a lock request that timed out (1222), and a snapshot
    // transaction that met a row another transaction changed (3960).
    private static readonly HashSet<int> _writeConflicts = [1205, 1222, 2601, 2627, 3960];

    // Errors that say nothing about the rows: a time-out of the client (-2), the victim of a deadlock (1205), a lock
    // request that timed out (1222), and a snapshot update conflict (3960). An exception that carries one of them is not
    // a data error, whatever else it carries.
    private static readonly HashSet<int> _transient = [-2, 1205, 1222, 3960];

    private readonly ILogger<SqlServerDatabaseProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerDatabaseProvider"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public SqlServerDatabaseProvider(ILogger<SqlServerDatabaseProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public bool SupportsPessimisticLocking => false;

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(databaseConfiguration);

        var customOptions = databaseConfiguration.CustomProviderOptions;
        if (string.IsNullOrWhiteSpace(customOptions?.ConnectionString))
        {
            throw new InvalidOperationException("No SQL Server connection string is configured. " + ConnectionStringGuidance);
        }

        var providerOptions = customOptions.Options;
        SqlConnectionStringBuilder connectionStringBuilder;
        try
        {
            connectionStringBuilder = ParseConnectionString(customOptions.ConnectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or KeyNotFoundException)
        {
            // Neither the connection string nor the exception is passed on, as the messages can repeat keywords and
            // values of the connection string, and it can hold a password.
            throw new InvalidOperationException(
                "The configured SQL Server connection string is not valid. It is not shown, as it may contain a password. "
                + ConnectionStringGuidance);
        }

        WarnOnceThatTheProviderIsExperimental(connectionStringBuilder);

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

        var connectionString = connectionStringBuilder.ConnectionString;
        var loggable = new SqlConnectionStringBuilder(connectionString) { Password = string.Empty };
        _logger.LogInformation("SQL Server connection string: {ConnectionString}", loggable.ConnectionString);

        options
            .UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions
                    .MigrationsAssembly(GetType().Assembly)
                    .UseCompatibilityLevel(CompatibilityLevel)
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                    .UseParameterizedCollectionMode(ParameterTranslationMode.MultipleParameters))
            .ReplaceService<IHistoryRepository, BinaryCollationHistoryRepository>()
            .AddInterceptors(SessionSettingsInterceptor.Instance);
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(
            options.Options.FindExtension<DateTimeConstantTranslator.Extension>() ?? new DateTimeConstantTranslator.Extension());

        var enableSensitiveDataLogging = GetOption(providerOptions, "EnableSensitiveDataLogging", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase));
        if (enableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
            _logger.LogInformation("EnableSensitiveDataLogging is enabled on SQL Server connection");
        }
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        AsciiCaseInsensitiveLike.Register(modelBuilder);
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder
            .Properties<Guid>()
            .HaveConversion<BigEndianGuidConverter>()
            .HaveColumnType(BigEndianGuidConverter.ColumnType);

        configurationBuilder.Conventions.Add(_ => new IdentifierLengthConvention(MaxIdentifierLength));
        configurationBuilder.Conventions.Add(_ => new IndexedTextConvention());
        configurationBuilder.Conventions.Add(_ => new UnindexedTextConvention());
        configurationBuilder.Conventions.Add(_ => new IndexFilterConvention());
        configurationBuilder.Conventions.Add(_ => new CascadePathConvention());

        // Last, as the conventions above can change the type of a column.
        configurationBuilder.Conventions.Add(services => new TextCollationConvention(services.GetRequiredService<IRelationalTypeMappingSource>()));
    }

    /// <inheritdoc/>
    public bool IsRetryableWriteConflict(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // 3960 is raised under snapshot isolation only, which Jellyfin does not use, so it is only retried in case a
        // transaction does.
        return ErrorNumbers(exception).Any(_writeConflicts.Contains);
    }

    /// <inheritdoc/>
    public bool IsDataError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var numbers = ErrorNumbers(exception);
        return numbers.Any(number => _dataErrors.Contains(number) || number >= FirstUserError) && !numbers.Any(_transient.Contains);
    }

    /// <inheritdoc/>
    public async ValueTask<IAsyncDisposable> BeginExplicitIdentityInsertAsync(JellyfinDbContext dbContext, IEntityType entityType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entityType);

        var table = entityType.GetTableName();
        if (table is null || !entityType.GetProperties().Any(p => p.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn))
        {
            return NoIdentityInsert.Instance;
        }

        // The setting belongs to the session, and a session may have it for one table at a time, so the connection stays
        // open until the scope ends, which turns it off again.
        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var on = "SET IDENTITY_INSERT " + QuoteIdentifier(table) + " ON";
            await dbContext.Database.ExecuteSqlRawAsync(on, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
        }

        var off = "SET IDENTITY_INSERT " + QuoteIdentifier(table) + " OFF";
        return new IdentityInsertScope(
            async () => await dbContext.Database.ExecuteSqlRawAsync(off).ConfigureAwait(false),
            async () => await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false),
            table,
            _logger);
    }

    /// <inheritdoc/>
    public Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        return UpdateStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshStatistics(CancellationToken cancellationToken)
    {
        return UpdateStatisticsAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task RunShutdownTask(CancellationToken cancellationToken)
    {
        SqlConnection.ClearAllPools();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        if (await HasAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "No backup of the SQL Server database is taken before migrating. A migration that fails is rolled back with "
                + "its transaction, but back the database up with BACKUP DATABASE before upgrading, it is the only way to return "
                + "to the previous version.");
        }

        // The provider has nothing to snapshot, the empty key says that no snapshot was taken.
        return string.Empty;
    }

    /// <inheritdoc/>
    public async Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            return;
        }

        var lastApplied = await GetLastAppliedMigrationAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogCritical(
            "There is no snapshot to restore on SQL Server. The failed migration was rolled back with its transaction, and the "
            + "database is at the last migration that applied completely ({Migration}). Restore the backup taken before the "
            + "upgrade to return to the previous version.",
            lastApplied ?? "none");
    }

    /// <inheritdoc/>
    public Task DeleteBackup(string key)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes every row of the given tables and starts their identity columns again at their first value, inside a
    /// transaction of the caller as well as without one.
    /// </summary>
    /// <remarks>
    /// Rows are deleted with the foreign keys of the tables switched off, as a table that other tables reference cannot be
    /// truncated, and the keys are checked again afterwards. An identity column goes on from its last value after a
    /// delete. Reseeding it to 0 makes the next value 1 for a table that has had rows, but 0 for a table that never had
    /// one, so only the identity columns that have handed out a value are reseeded. All of it is part of the transaction.
    /// </remarks>
    /// <param name="dbContext">The Database context.</param>
    /// <param name="tableNames">The names of the tables to purge. It must not be <see langword="null"/>.</param>
    /// <returns>A Task.</returns>
    public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(tableNames);

        var tables = tableNames.ToList();
        if (tables.Count == 0)
        {
            return;
        }

        if (dbContext.Database.CurrentTransaction is not null)
        {
            await PurgeTablesAsync(dbContext, tables).ConfigureAwait(false);
            return;
        }

        var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            await PurgeTablesAsync(dbContext, tables).ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Warns when the database reads at read committed with locks, and when its default collation is not the one of the
    /// text columns. Neither is changed here, as both changes need exclusive access to the database.
    /// </summary>
    /// <param name="dbContext">A context on the database that is in use.</param>
    /// <param name="cancellationToken">The token to abort the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ApplyDatabaseSettingsAsync(JellyfinDbContext dbContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        try
        {
            // Each value is read on its own: the string columns of the catalog have collations of their own, and
            // comparing or joining them can fail with a conflict of collations.
            var database = await dbContext.Database
                .SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]")
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);
            var readCommittedSnapshot = await dbContext.Database
                .SqlQueryRaw<bool>("SELECT is_read_committed_snapshot_on AS [Value] FROM sys.databases WHERE database_id = DB_ID()")
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);
            var collation = await dbContext.Database
                .SqlQueryRaw<string>("SELECT CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation')) AS [Value]")
                .SingleAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!readCommittedSnapshot)
            {
                _logger.LogWarning(
                    "READ_COMMITTED_SNAPSHOT is off for the database {Database}, so reads wait for the writes of a library scan. "
                    + "Run ALTER DATABASE {QuotedDatabase} SET READ_COMMITTED_SNAPSHOT ON while Jellyfin is stopped, as it needs "
                    + "exclusive access to the database.",
                    database,
                    QuoteIdentifier(database));
            }

            if (!string.Equals(collation, Collation, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "The database {Database} has the default collation {Collation}. Jellyfin gives every column it creates the "
                    + "collation {Expected}, but text that is not a column, and the names of tables and columns, compare under the "
                    + "default. Create the database with COLLATE {Expected}.",
                    database,
                    collation,
                    Collation,
                    Collation);
            }
        }
#pragma warning disable CA1031 // A check that cannot run must not stop the server from starting; it is logged.
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            _logger.LogWarning(ex, "Could not read the settings of the SQL Server database");
        }
#pragma warning restore CA1031
    }

    private static T? GetOption<T>(ICollection<CustomDatabaseOption>? options, string key, Func<string, T> converter)
    {
        var value = options?.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return value is null ? default : converter(value.Value);
    }

    // Encryption is mandatory unless the connection string names Encrypt. SqlClient 6 defaults to Mandatory as well, so
    // nothing changes at runtime today: the provider states the setting, and the log shows it, instead of relying on the
    // default of the driver. An Encrypt the operator wrote, such as Encrypt=False for a server on the same machine, is kept.
    // CodeQL's cs/insecure-sql-connection still reports this builder. Its only accepted form is `Encrypt = true` as a bool
    // literal in the initializer, which SqlConnectionEncryptOption cannot match and which would override the operator's
    // choice, so the report is a false positive.
    private static SqlConnectionStringBuilder ParseConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.ShouldSerialize("Encrypt"))
        {
            builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
        }

        return builder;
    }

    private static bool SpecifiesCommandTimeout(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.ContainsKey("Command Timeout") || builder.ContainsKey("CommandTimeout");
    }

    // The numbers of every error of the first SqlException in the chain. The server sends the cause of a refused statement
    // with "The statement has been terminated." (3621) after it, and a batch of several statements can carry several
    // errors, so the cause need not be the first error; every one is looked at.
    private static List<int> ErrorNumbers(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                var numbers = sqlException.Errors.Cast<SqlError>().Select(error => error.Number).ToList();
                return numbers.Count > 0 ? numbers : [sqlException.Number];
            }
        }

        return [];
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    private static string QuoteString(string value)
    {
        return "N'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static Task<bool> HasLibraryItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Folders and the seeded placeholder exist before any library has been scanned.
        return context.BaseItems.AnyAsync(e => !e.IsFolder && e.Type != "PLACEHOLDER", cancellationToken);
    }

    private static async Task PurgeTablesAsync(JellyfinDbContext dbContext, List<string> tables)
    {
        foreach (var table in tables)
        {
            var noCheck = "ALTER TABLE " + QuoteIdentifier(table) + " NOCHECK CONSTRAINT ALL";
            await dbContext.Database.ExecuteSqlRawAsync(noCheck).ConfigureAwait(false);
        }

        foreach (var table in tables)
        {
            var delete = "DELETE FROM " + QuoteIdentifier(table);
            await dbContext.Database.ExecuteSqlRawAsync(delete).ConfigureAwait(false);
        }

        foreach (var table in tables)
        {
            var check = "ALTER TABLE " + QuoteIdentifier(table) + " WITH CHECK CHECK CONSTRAINT ALL";
            await dbContext.Database.ExecuteSqlRawAsync(check).ConfigureAwait(false);
        }

        var used = await dbContext.Database
            .SqlQueryRaw<string>("SELECT OBJECT_NAME(object_id) AS [Value] FROM sys.identity_columns WHERE last_value IS NOT NULL AND OBJECTPROPERTY(object_id, 'IsUserTable') = 1")
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var table in used.Where(table => tables.Contains(table, StringComparer.OrdinalIgnoreCase)))
        {
            var reseed = "DBCC CHECKIDENT (" + QuoteString(QuoteIdentifier(table)) + ", RESEED, 0) WITH NO_INFOMSGS";
            await dbContext.Database.ExecuteSqlRawAsync(reseed).ConfigureAwait(false);
        }
    }

    private void WarnOnceThatTheProviderIsExperimental(SqlConnectionStringBuilder builder)
    {
        // The password is not part of the key, as the key is kept for the life of the process.
        var database = builder.DataSource + "/" + builder.InitialCatalog;
        if (_databasesWarnedOf.TryAdd(database, true))
        {
            _logger.LogWarning("The SQL Server database provider is experimental. Back up the data before using it with a library you cannot rebuild.");
        }
    }

    private async Task UpdateStatisticsAsync(CancellationToken cancellationToken)
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
                _logger.LogInformation("Not updating the statistics of the database, the library holds no items yet");
                return;
            }

            _logger.LogInformation("Updating the statistics of the database");
            var tables = context.Model.GetEntityTypes()
                .Select(e => e.GetTableName())
                .OfType<string>()
                .Distinct(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                var update = "UPDATE STATISTICS " + QuoteIdentifier(table);
                await context.Database.ExecuteSqlRawAsync(update, cancellationToken).ConfigureAwait(false);
            }
        }
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

    private sealed class NoIdentityInsert : IAsyncDisposable
    {
        public static readonly NoIdentityInsert Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// Turns IDENTITY_INSERT off and releases the connection when the rows of a table are inserted.
    /// </summary>
    /// <param name="turnOff">Turns the setting off.</param>
    /// <param name="close">Releases the connection that was held open for the scope.</param>
    /// <param name="table">The table.</param>
    /// <param name="logger">The logger.</param>
    internal sealed class IdentityInsertScope(Func<Task> turnOff, Func<Task> close, string table, ILogger logger) : IAsyncDisposable
    {
        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            // The scope ends with await using, also when the inserts failed, often on a connection that is broken or
            // closed by then. An error here would replace the error of the inserts, which is the one that says what went
            // wrong, so it is only logged.
            try
            {
                await turnOff().ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Any error of ending the scope is logged, so that it cannot hide the error of the inserts.
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                logger.LogWarning(ex, "Could not turn IDENTITY_INSERT off for {Table}", table);
            }
#pragma warning restore CA1031
            finally
            {
                try
                {
                    await close().ConfigureAwait(false);
                }
#pragma warning disable CA1031 // As above.
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    logger.LogWarning(ex, "Could not release the connection after inserting into {Table}", table);
                }
#pragma warning restore CA1031
            }
        }
    }
}
